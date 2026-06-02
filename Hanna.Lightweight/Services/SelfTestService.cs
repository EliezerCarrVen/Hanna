using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class SelfTestService(
    RuntimePaths paths,
    LightweightStartupService startup,
    FlatFileMemoryService memory,
    MarkdownVaultService markdown,
    CodeCacheService codeCache,
    RipgrepSearchService search,
    AuditLogService auditLog)
{
    public async Task<IReadOnlyList<CheckResult>> RunAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<CheckResult>();
        await RunStep(results, "startup creates HannaData", async () =>
        {
            await startup.StartAsync(cancellationToken);
            return Directory.Exists(paths.DataRoot);
        });
        await RunStep(results, "validate paths", () => Task.FromResult(Directory.Exists(paths.Vault) && Directory.Exists(paths.Runtime) && Directory.Exists(paths.Indexes) && Directory.Exists(paths.Logs)));
        await RunStep(results, "write JSONL test", async () =>
        {
            await memory.AddShortMemoryAsync("self-test", "self-test memoria prueba jwt", ["self-test", "prueba"], cancellationToken);
            return File.Exists(paths.ShortMemory);
        });
        await RunStep(results, "read recent memory", async () => (await memory.ReadRecentShortMemoryAsync(10, cancellationToken)).Count > 0);
        await RunStep(results, "create Markdown note", async () => File.Exists(await markdown.CreateMemoryNoteAsync("Self-test prueba", "Nota Markdown de self-test prueba.", cancellationToken)));
        await RunStep(results, "create code cache", async () => File.Exists(await codeCache.CreateTestCodeCacheAsync(cancellationToken)));
        await RunStep(results, "search prueba", async () => (await search.SearchAsync(paths.Vault, "prueba", cancellationToken)).Count > 0);
        await RunStep(results, "search jwt", async () => (await search.SearchAsync(paths.VaultCodigoCache, "jwt", cancellationToken)).Count > 0);
        await RunStep(results, "audit dry-run", async () =>
        {
            await auditLog.RecordAsync("self_test", "Self-test executed in dry-run mode.", true, "info", cancellationToken);
            return File.Exists(paths.AuditLog);
        });
        await RunStep(results, "verify logs", () => Task.FromResult(File.Exists(paths.LightweightLog) && File.Exists(paths.AuditLog)));
        await auditLog.RecordAsync("self_test_completed", $"Self-test global status {DoctorService.GetGlobalStatus(results)}.", true, "info", cancellationToken);
        return results;
    }

    private static async Task RunStep(List<CheckResult> results, string name, Func<Task<bool>> step)
    {
        try
        {
            results.Add(new CheckResult(name, await step() ? "PASS" : "FAIL", name));
        }
        catch (Exception ex)
        {
            results.Add(new CheckResult(name, "FAIL", ex.Message));
        }
    }
}
