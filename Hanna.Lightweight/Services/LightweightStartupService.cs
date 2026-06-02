using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class LightweightStartupService(
    LightweightOptions options,
    RuntimePaths paths,
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
            Directory.CreateDirectory(directory);
        }

        foreach (var file in paths.Files)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file) ?? paths.DataRoot);
            if (!File.Exists(file))
            {
                await File.WriteAllTextAsync(file, file.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? "# Rolling summary\n\nPendiente de generar.\n" : string.Empty, cancellationToken);
            }
        }

        await LogAsync("Hanna Lightweight startup completed.", cancellationToken);
        await memoryService.AddShortMemoryAsync("startup", "entrada de prueba inicial de Hanna Lightweight", ["startup", "prueba"], cancellationToken);
        await markdownVault.CreateMemoryNoteAsync("Nota de prueba inicial", "Memoria Markdown de prueba creada al arrancar Hanna Lightweight.", cancellationToken);
        await auditLog.RecordAsync("startup_sensitive_modules", "Módulos sensibles permanecen en DryRun=true y planned_not_implemented.", true, "info", cancellationToken);

        return new StartupReport(options.Mode, options.MemoryMode, paths.Vault, paths.ShortMemory, searchService.IsRipgrepAvailable, modules.GetModules());
    }

    public Task LogAsync(string message, CancellationToken cancellationToken = default) =>
        File.AppendAllTextAsync(paths.LightweightLog, $"{DateTimeOffset.UtcNow:O} {message}{Environment.NewLine}", cancellationToken);
}
