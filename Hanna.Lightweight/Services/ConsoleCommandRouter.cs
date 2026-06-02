using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class ConsoleCommandRouter(
    LightweightOptions options,
    RuntimePaths paths,
    FlatFileMemoryService memory,
    MarkdownVaultService markdownVault,
    CodeCacheService codeCache,
    RipgrepSearchService search,
    ModuleRegistryService modules,
    AuditLogService auditLog,
    LightweightStartupService startup)
{
    public async Task<bool> HandleAsync(string? command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return true;
        }

        var trimmed = command.Trim();
        if (trimmed.Equals("/salir", StringComparison.OrdinalIgnoreCase))
        {
            await startup.LogAsync("Console exit requested.", cancellationToken);
            Console.WriteLine("Cerrando Hanna Lightweight.");
            return false;
        }

        if (trimmed.Equals("/status", StringComparison.OrdinalIgnoreCase))
        {
            PrintStatus();
            return true;
        }

        if (trimmed.Equals("/memoria prueba", StringComparison.OrdinalIgnoreCase))
        {
            await memory.AddShortMemoryAsync("console", "memoria prueba desde consola", ["memoria", "prueba"], cancellationToken);
            var notePath = await markdownVault.CreateMemoryNoteAsync("Memoria prueba consola", "Contenido de prueba para búsqueda local en vault.", cancellationToken);
            await startup.LogAsync($"Memory test created: {notePath}", cancellationToken);
            Console.WriteLine($"Memoria de prueba guardada: {notePath}");
            foreach (var line in await memory.ReadRecentShortMemoryAsync(options.LastEntriesToRead, cancellationToken))
            {
                Console.WriteLine(line);
            }
            return true;
        }

        if (trimmed.StartsWith("/memoria buscar ", StringComparison.OrdinalIgnoreCase))
        {
            var term = trimmed["/memoria buscar ".Length..];
            await PrintSearchAsync(paths.Vault, term, cancellationToken);
            return true;
        }

        if (trimmed.Equals("/codigo prueba", StringComparison.OrdinalIgnoreCase))
        {
            var path = await codeCache.CreateTestCodeCacheAsync(cancellationToken);
            await auditLog.RecordAsync("code_cache_test", "Creación simulada segura de caché de código; sin secretos.", true, "info", cancellationToken);
            await startup.LogAsync($"Code cache test created: {path}", cancellationToken);
            Console.WriteLine($"Caché de código de prueba guardada: {path}");
            return true;
        }

        if (trimmed.StartsWith("/codigo buscar ", StringComparison.OrdinalIgnoreCase))
        {
            var term = trimmed["/codigo buscar ".Length..];
            await PrintSearchAsync(paths.VaultCodigoCache, term, cancellationToken);
            return true;
        }

        if (trimmed.Equals("/modulos", StringComparison.OrdinalIgnoreCase))
        {
            PrintModules();
            return true;
        }

        if (trimmed.Equals("/auditoria", StringComparison.OrdinalIgnoreCase))
        {
            var events = await auditLog.ReadRecentAsync(10, cancellationToken);
            Console.WriteLine("Últimos eventos de auditoría:");
            foreach (var item in events)
            {
                Console.WriteLine(item);
            }
            return true;
        }

        Console.WriteLine("Comando no reconocido. Usa /status, /memoria prueba, /memoria buscar TEXTO, /codigo prueba, /codigo buscar TEXTO, /modulos, /auditoria o /salir.");
        return true;
    }

    private void PrintStatus()
    {
        Console.WriteLine("Estado Hanna.Lightweight");
        Console.WriteLine($"modo: {options.Mode}");
        Console.WriteLine($"memoria: {options.MemoryMode}");
        Console.WriteLine($"data root: {paths.DataRoot}");
        Console.WriteLine($"vault path: {paths.Vault}");
        Console.WriteLine($"short memory path: {paths.ShortMemory}");
        Console.WriteLine($"ripgrep: {(search.IsRipgrepAvailable ? "disponible" : "no disponible; fallback C#")}");
        PrintModules();
    }

    private void PrintModules()
    {
        Console.WriteLine("Módulos:");
        foreach (var module in modules.GetModules())
        {
            Console.WriteLine($"- {module.Name}: {module.Status} (DryRun={module.DryRun}) - {module.Notes}");
        }
    }

    private async Task PrintSearchAsync(string root, string term, CancellationToken cancellationToken)
    {
        var results = await search.SearchAsync(root, term, cancellationToken);
        Console.WriteLine($"Resultados para '{term}' en {root}: {results.Count}");
        foreach (var result in results)
        {
            Console.WriteLine($"- [{result.Engine}] {result.FilePath}:{result.LineNumber}: {result.Preview}");
        }
    }
}
