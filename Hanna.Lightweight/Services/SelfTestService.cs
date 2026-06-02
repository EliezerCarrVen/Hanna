using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class SelfTestService(
    RuntimePaths paths,
    LightweightStartupService startup,
    FlatFileMemoryService memory,
    MarkdownVaultService markdown,
    CodeCacheService codeCache,
    RipgrepSearchService search,
    AuditLogService auditLog,
    PathGuardService pathGuard,
    SecretFilterService secretFilter,
    VaultEncryptionService vault,
    TotpService totp,
    RbacService rbac,
    NasIndexerService nas,
    ZeroLeakSanitizerService zeroLeak,
    IntentRouterService intentRouter,
    WakeOnLanService wol,
    ExternalToolModuleService externalTools,
    RollingSummaryService summary,
    VaultIndexService index)
{
    public async Task<IReadOnlyList<CheckResult>> RunAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<CheckResult>();
        await RunStep(results, "startup creates HannaData", async () => { await startup.StartAsync(cancellationToken); return Directory.Exists(paths.DataRoot); });
        await RunStep(results, "PathGuard blocks outside root", () => Task.FromResult(!pathGuard.IsInsideRoot(Path.GetTempPath())));
        await RunStep(results, "SecretFilter redacts token", () => Task.FromResult(secretFilter.Filter("token=abc123").Contains("[REDACTED]", StringComparison.Ordinal)));
        await RunStep(results, "write JSONL test", async () => { await memory.AddShortMemoryAsync("self-test", "self-test memoria prueba jwt", ["self-test", "prueba"], cancellationToken); return File.Exists(paths.ShortMemory); });
        await RunStep(results, "read recent memory", async () => (await memory.ReadRecentShortMemoryAsync(10, cancellationToken)).Count > 0);
        await RunStep(results, "create Markdown note", async () => File.Exists(await markdown.CreateMemoryNoteAsync("Self-test prueba", "Nota Markdown de self-test prueba.", cancellationToken)));
        await RunStep(results, "create code cache", async () => File.Exists(await codeCache.CreateTestCodeCacheAsync(cancellationToken)));
        await RunStep(results, "search prueba", async () => (await search.SearchAsync(paths.Vault, "prueba", cancellationToken)).Count > 0);
        await RunStep(results, "search jwt", async () => (await search.SearchAsync(paths.VaultCodigoCache, "jwt", cancellationToken)).Count > 0);
        await RunStep(results, "audit dry-run", async () => { await auditLog.RecordAsync("self_test", "Self-test executed in dry-run mode.", true, "info", cancellationToken); return File.Exists(paths.AuditLog); });
        await RunStep(results, "audit hash-chain", () => Task.FromResult(auditLog.VerifyHashChain().ok));
        await RunStep(results, "vault encryption local", async () => !string.IsNullOrWhiteSpace(await vault.CreateAsync("selftest", "selftest-password", cancellationToken)));
        results.Add(new CheckResult("TOTP status", totp.Status().Status == "implemented" ? "PASS" : "WARN", totp.Status().Message));
        await RunStep(results, "RBAC local", () => Task.FromResult(rbac.Roles.Contains("root")));
        results.Add(new CheckResult("NAS dry-run", nas.Status().StartsWith("missing_configuration", StringComparison.Ordinal) ? "WARN" : "PASS", nas.Status()));
        await RunStep(results, "ZeroLeak", () => Task.FromResult(zeroLeak.Sanitize("mail test@example.com ip 192.168.1.5 token=abc").Contains("[REDACTED]", StringComparison.Ordinal)));
        await RunStep(results, "IntentRouter", () => Task.FromResult(intentRouter.Route("publicar mqtt").intent == "mqtt"));
        await RunStep(results, "WOL dry-run", () => Task.FromResult(wol.Send("00:11:22:33:44:55", false).Contains("dry_run", StringComparison.Ordinal)));
        results.Add(new CheckResult("MQTT dry-run", externalTools.MqttStatus().StartsWith("missing_", StringComparison.Ordinal) ? "WARN" : "PASS", externalTools.MqttStatus()));
        results.Add(new CheckResult("Docker dependency check", externalTools.DockerStatus().StartsWith("missing_", StringComparison.Ordinal) ? "WARN" : "PASS", externalTools.DockerStatus()));
        results.Add(new CheckResult("ClamAV dependency check", externalTools.ClamAvStatus().StartsWith("missing_", StringComparison.Ordinal) ? "WARN" : "PASS", externalTools.ClamAvStatus()));
        results.Add(new CheckResult("Node-RED config check", externalTools.NodeRedStatus().StartsWith("missing_", StringComparison.Ordinal) ? "WARN" : "PASS", externalTools.NodeRedStatus()));
        await RunStep(results, "Summary", async () => File.Exists(await summary.RegenerateAsync(cancellationToken)));
        await RunStep(results, "Index", async () => await index.RebuildAsync(cancellationToken) >= 0);
        await RunStep(results, "verify logs", () => Task.FromResult(File.Exists(paths.LightweightLog) && File.Exists(paths.AuditLog)));
        await auditLog.RecordAsync("self_test_completed", $"Self-test global status {DoctorService.GetGlobalStatus(results)}.", true, "info", cancellationToken);
        return results;
    }

    private static async Task RunStep(List<CheckResult> results, string name, Func<Task<bool>> step)
    {
        try { results.Add(new CheckResult(name, await step() ? "PASS" : "FAIL", name)); }
        catch (Exception ex) { results.Add(new CheckResult(name, "FAIL", ex.Message)); }
    }
}
