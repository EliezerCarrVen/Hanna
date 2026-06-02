using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class LightweightStartupService(
    LightweightOptions options,
    RuntimePaths paths,
    PathGuardService pathGuard,
    LogRotationService logRotation,
    FlatFileMemoryService memoryService,
    MarkdownVaultService markdownVault,
    AuditLogService auditLog,
    ModuleRegistryService modules,
    RipgrepSearchService searchService)
{
    public async Task<StartupReport> StartAsync(CancellationToken cancellationToken = default)
    {
        foreach (var directory in paths.Directories)
        {
            pathGuard.EnsureInsideRoot(directory);
            Directory.CreateDirectory(directory);
        }

        foreach (var file in paths.Files)
        {
            var safeFile = pathGuard.EnsureInsideRoot(file);
            Directory.CreateDirectory(Path.GetDirectoryName(safeFile) ?? paths.DataRoot);
            if (!File.Exists(safeFile))
            {
                await File.WriteAllTextAsync(safeFile, safeFile.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? "# Rolling summary\n\nPendiente de generar.\n" : string.Empty, cancellationToken);
            }
        }

        logRotation.RotateKnownLogs();
        await LogAsync("Hanna Lightweight startup completed.", cancellationToken);
        await memoryService.AddShortMemoryAsync("startup", "entrada de prueba inicial de Hanna Lightweight", ["startup", "prueba"], cancellationToken);
        await markdownVault.CreateMemoryNoteAsync("Nota de prueba inicial", "Memoria Markdown de prueba creada al arrancar Hanna Lightweight.", cancellationToken);
        await auditLog.RecordAsync("startup_sensitive_modules", "Módulos sensibles permanecen en DryRun=true y planned_not_implemented.", true, "info", cancellationToken);

        return new StartupReport(options.Mode, options.MemoryMode, paths.Vault, paths.ShortMemory, searchService.IsRipgrepAvailable, modules.GetModules());
    }

    public Task LogAsync(string message, CancellationToken cancellationToken = default)
    {
        var safePath = pathGuard.EnsureInsideRoot(paths.LightweightLog);
        logRotation.RotateIfNeeded(safePath);
        return File.AppendAllTextAsync(safePath, $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}", cancellationToken);
    }
}
