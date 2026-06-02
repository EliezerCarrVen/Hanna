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
    LightweightStartupService startup,
    DoctorService doctor,
    SelfTestService selfTest,
    RollingSummaryService summary,
    VaultIndexService vaultIndex)
{
    public async Task<bool> HandleAsync(string? command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return true;
        }

        if (command.Length > options.MaxCommandLength)
        {
            Console.WriteLine($"FAIL comando demasiado largo. Límite: {options.MaxCommandLength} caracteres.");
            await auditLog.RecordAsync("command_rejected", "Command rejected because it exceeded MaxCommandLength.", true, "warn", cancellationToken);
            return true;
        }

        var trimmed = command.Trim();
        await auditLog.RecordCommandAsync(trimmed, cancellationToken);
        if (trimmed.Equals("/salir", StringComparison.OrdinalIgnoreCase))
        {
            await startup.LogAsync("Console exit requested.", cancellationToken);
            Console.WriteLine("Cerrando Hanna Lightweight.");
            return false;
        }

        if (trimmed.Equals("/help", StringComparison.OrdinalIgnoreCase))
        {
            PrintHelp();
            return true;
        }

        if (trimmed.Equals("/status", StringComparison.OrdinalIgnoreCase))
        {
            await PrintStatusAsync(cancellationToken);
            return true;
        }

        if (trimmed.Equals("/doctor", StringComparison.OrdinalIgnoreCase))
        {
            PrintChecks(await doctor.RunAsync(cancellationToken));
            return true;
        }

        if (trimmed.Equals("/selftest", StringComparison.OrdinalIgnoreCase))
        {
            PrintChecks(await selfTest.RunAsync(cancellationToken));
            return true;
        }

        if (trimmed.Equals("/summary", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("/summary regenerar", StringComparison.OrdinalIgnoreCase))
        {
            var summaryPath = await summary.RegenerateAsync(cancellationToken);
            Console.WriteLine($"PASS summary actualizado: {summaryPath}");
            return true;
        }

        if (trimmed.Equals("/indexar", StringComparison.OrdinalIgnoreCase))
        {
            var count = await vaultIndex.RebuildAsync(cancellationToken);
            Console.WriteLine($"PASS vault indexado: {count} archivo(s)");
            return true;
        }

        if (trimmed.Equals("/indice estado", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(vaultIndex.GetStatus());
            return true;
        }

        if (trimmed.Equals("/memoria prueba", StringComparison.OrdinalIgnoreCase))
        {
            await memory.AddShortMemoryAsync("console", "memoria prueba desde consola", ["memoria", "prueba"], cancellationToken);
            var notePath = await markdownVault.CreateMemoryNoteAsync("Memoria prueba consola", "Contenido de prueba para búsqueda local en vault.", cancellationToken);
            await auditLog.RecordAsync("memory_note_created", $"Nota de memoria creada: {Path.GetFileName(notePath)}", true, "info", cancellationToken);
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

        if (trimmed.Equals("/codigo listar", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var entry in codeCache.ListEntries())
            {
                Console.WriteLine(entry);
            }
            return true;
        }

        if (trimmed.Equals("/codigo estado", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(codeCache.GetStatus());
            return true;
        }

        if (trimmed.Equals("/modulos", StringComparison.OrdinalIgnoreCase))
        {
            PrintModules();
            return true;
        }

        if (trimmed.Equals("/auditoria", StringComparison.OrdinalIgnoreCase))
        {
            var events = await auditLog.ReadRecentAsync(options.MaxAuditEventsToRead, cancellationToken);
            Console.WriteLine("Últimos eventos de auditoría:");
            foreach (var item in events)
            {
                Console.WriteLine(item);
            }
            return true;
        }

        Console.WriteLine("Comando no reconocido. Usa /help para ver comandos disponibles.");
        return true;
    }

    private async Task PrintStatusAsync(CancellationToken cancellationToken)
    {
        var checks = await doctor.RunAsync(cancellationToken);
        var moduleList = modules.GetModules();
        Console.WriteLine("Estado Hanna.Lightweight");
        Console.WriteLine($"modo: {options.Mode}");
        Console.WriteLine($"memoria: {options.MemoryMode}");
        Console.WriteLine($"data root: {paths.DataRoot}");
        Console.WriteLine($"vault path: {paths.Vault}");
        Console.WriteLine($"short memory path: {paths.ShortMemory}");
        Console.WriteLine($"ripgrep: {(search.IsRipgrepAvailable ? "disponible" : "no disponible; fallback C#")}");
        Console.WriteLine($"short_memory_entries_aprox: {memory.CountShortMemoryEntries()}");
        Console.WriteLine($"markdown_notes_aprox: {markdownVault.CountMarkdownNotes(paths.Vault)}");
        Console.WriteLine($"code_cache_notes_aprox: {markdownVault.CountMarkdownNotes(paths.VaultCodigoCache)}");
        Console.WriteLine($"logs_size_bytes: {GetSize(paths.LightweightLog) + GetSize(paths.AuditLog) + GetSize(paths.SecurityLog)}");
        Console.WriteLine($"modules_implemented: {moduleList.Count(module => module.Status == "implemented")}");
        Console.WriteLine($"modules_partial: {moduleList.Count(module => module.Status == "partial" || module.Status == "fallback")}");
        Console.WriteLine($"modules_planned_not_implemented: {moduleList.Count(module => module.Status == "planned_not_implemented")}");
        Console.WriteLine($"global_status: {DoctorService.GetGlobalStatus(checks)}");
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

    private static void PrintChecks(IReadOnlyList<CheckResult> checks)
    {
        foreach (var check in checks)
        {
            Console.WriteLine($"{check.Status} {check.Name}: {check.Message}");
        }
        Console.WriteLine($"GLOBAL {DoctorService.GetGlobalStatus(checks)}");
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Comandos disponibles:");
        foreach (var command in new[]
        {
            "/status", "/doctor", "/selftest", "/memoria prueba", "/memoria buscar TEXTO",
            "/codigo prueba", "/codigo buscar TEXTO", "/codigo listar", "/codigo estado",
            "/summary", "/summary regenerar", "/indexar", "/indice estado", "/modulos", "/auditoria", "/salir"
        })
        {
            Console.WriteLine($"- {command}");
        }
    }

    private static long GetSize(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;
}
