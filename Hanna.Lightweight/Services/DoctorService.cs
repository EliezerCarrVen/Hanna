using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed record CheckResult(string Name, string Status, string Message);

public sealed class DoctorService(
    RuntimePaths paths,
    PathGuardService pathGuard,
    RipgrepSearchService search,
    ModuleRegistryService modules,
    LightweightOptions options,
    AuditLogService auditLog)
{
    public async Task<IReadOnlyList<CheckResult>> RunAsync(CancellationToken cancellationToken = default)
    {
        var checks = new List<CheckResult>
        {
            ExistsDirectory("HannaData", paths.DataRoot),
            ExistsDirectory("vault", paths.Vault),
            ExistsDirectory("runtime", paths.Runtime),
            ExistsDirectory("indexes", paths.Indexes),
            ExistsDirectory("logs", paths.Logs),
            CanWrite(paths.Runtime),
            ExistsFile("short_memory.jsonl", paths.ShortMemory),
            ExistsFile("audit.log", paths.AuditLog),
            ExistsFile("lightweight.log", paths.LightweightLog),
            new("ripgrep", search.IsRipgrepAvailable ? "PASS" : "WARN", search.IsRipgrepAvailable ? "rg disponible" : "rg no disponible; fallback C# activo"),
            new("configuración", options.DangerousModulesDryRun ? "PASS" : "FAIL", $"DryRun={options.DangerousModulesDryRun}"),
            new("logs size", LogsWithinLimit() ? "PASS" : "WARN", BuildLogSizeMessage()),
            new("modules", modules.GetModules().Any(m => m.DryRun && m.Status == "planned_not_implemented") ? "PASS" : "WARN", "módulos peligrosos permanecen planificados/dry-run"),
            GitignoreHasHannaData()
        };

        await auditLog.RecordAsync("doctor", $"Doctor executed with global status {GetGlobalStatus(checks)}.", true, "info", cancellationToken);
        return checks;
    }

    public static string GetGlobalStatus(IEnumerable<CheckResult> checks)
    {
        var list = checks.ToArray();
        if (list.Any(check => check.Status.Equals("FAIL", StringComparison.OrdinalIgnoreCase)))
        {
            return "FAIL";
        }

        return list.Any(check => check.Status.Equals("WARN", StringComparison.OrdinalIgnoreCase)) ? "WARN" : "PASS";
    }

    private CheckResult ExistsDirectory(string name, string path)
    {
        try
        {
            pathGuard.EnsureInsideRoot(path);
            return new CheckResult(name, Directory.Exists(path) ? "PASS" : "FAIL", path);
        }
        catch (Exception ex)
        {
            return new CheckResult(name, "FAIL", ex.Message);
        }
    }

    private CheckResult ExistsFile(string name, string path)
    {
        try
        {
            pathGuard.EnsureInsideRoot(path);
            return new CheckResult(name, File.Exists(path) ? "PASS" : "FAIL", path);
        }
        catch (Exception ex)
        {
            return new CheckResult(name, "FAIL", ex.Message);
        }
    }

    private CheckResult CanWrite(string directory)
    {
        try
        {
            var testPath = pathGuard.EnsureInsideRoot(Path.Combine(directory, $".write-test-{Guid.NewGuid():N}.tmp"));
            File.WriteAllText(testPath, "ok");
            File.Delete(testPath);
            return new CheckResult("write permissions", "PASS", directory);
        }
        catch (Exception ex)
        {
            return new CheckResult("write permissions", "FAIL", ex.Message);
        }
    }

    private bool LogsWithinLimit() => GetKnownLogSizes().All(item => item.size <= options.MaxLogFileBytes);

    private string BuildLogSizeMessage() => string.Join(", ", GetKnownLogSizes().Select(item => $"{Path.GetFileName(item.path)}={item.size} bytes"));

    private IEnumerable<(string path, long size)> GetKnownLogSizes()
    {
        foreach (var log in new[] { paths.LightweightLog, paths.AuditLog, paths.SecurityLog })
        {
            yield return (log, File.Exists(log) ? new FileInfo(log).Length : 0);
        }
    }

    private static CheckResult GitignoreHasHannaData()
    {
        var gitignore = Path.Combine(Environment.CurrentDirectory, ".gitignore");
        if (!File.Exists(gitignore))
        {
            return new CheckResult(".gitignore HannaData", "WARN", ".gitignore no encontrado");
        }

        var found = File.ReadLines(gitignore).Any(line => line.Trim().Equals("HannaData/", StringComparison.OrdinalIgnoreCase));
        return new CheckResult(".gitignore HannaData", found ? "PASS" : "WARN", found ? "HannaData/ está ignorado" : "HannaData/ no aparece en .gitignore");
    }
}
