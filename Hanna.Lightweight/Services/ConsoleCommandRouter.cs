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
    VaultIndexService vaultIndex,
    DependencyCheckerService deps,
    ZeroLeakSanitizerService zeroLeak,
    IntentRouterService intentRouter,
    WakeOnLanService wol,
    TotpService totp,
    VaultEncryptionService vaultCrypto,
    NetworkAccessPolicyService networkPolicy,
    RbacService rbac,
    ExternalToolModuleService externalTools,
    NasIndexerService nas,
    CodeTranslationPlannerService translations,
    SystemDiagnosticsService systemDiagnostics,
    PlannerStatusService planners)
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


        if (trimmed.Equals("/deps", StringComparison.OrdinalIgnoreCase) || trimmed.Equals("/deps instalar sugerencias", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var d in deps.CheckAll()) Console.WriteLine($"{d.Name}: {(d.Found ? "found" : "missing_dependency")} path={d.Path ?? "-"} version={d.Version ?? "-"} debian='{d.DebianInstall}' windows='{d.WindowsInstall}'");
            return true;
        }

        if (trimmed.StartsWith("/vault ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = trimmed.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries);
            var action = parts.ElementAtOrDefault(1) ?? "estado";
            if (action == "estado" || action == "doctor") Console.WriteLine(vaultCrypto.Status());
            else if (action == "listar" || action == "map") Console.WriteLine(vaultCrypto.List());
            else if (action == "verificar") Console.WriteLine(vaultCrypto.Verify());
            else if (action == "crear") Console.WriteLine($"PASS vault creado: {await vaultCrypto.CreateAsync(parts.ElementAtOrDefault(2) ?? "vault", ReadSecret("Contraseña maestra local: "), cancellationToken)}");
            else if (action == "importar") Console.WriteLine($"PASS importado GUID: {await vaultCrypto.ImportAsync(parts.ElementAtOrDefault(2) ?? string.Empty, ReadSecret("Contraseña maestra local: "), cancellationToken)}");
            else if (action == "exportar") Console.WriteLine("unsafe_without_confirmation: exportar requiere destino allowlist y confirmación explícita");
            else if (action == "buscar") Console.WriteLine(vaultCrypto.List().Contains(parts.ElementAtOrDefault(2) ?? "", StringComparison.OrdinalIgnoreCase) ? "found" : "not_found");
            return true;
        }

        if (trimmed.StartsWith("/totp ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = trimmed.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            if (parts.ElementAtOrDefault(1) == "estado") Console.WriteLine($"{totp.Status().Status}: {totp.Status().Message}");
            else if (parts.ElementAtOrDefault(1) == "generar-secreto") Console.WriteLine($"Secreto TOTP generado (guárdalo en tu app 2FA): {totp.GenerateSecret()}");
            else if (parts.ElementAtOrDefault(1) == "verificar") Console.WriteLine(totp.Verify(parts.ElementAtOrDefault(2) ?? "") ? "PASS código válido" : "FAIL código inválido o missing_configuration");
            return true;
        }

        if (trimmed.StartsWith("/red whitelist ", StringComparison.OrdinalIgnoreCase))
        {
            var parts = trimmed.Split(' ', 4, StringSplitOptions.RemoveEmptyEntries); var action = parts.ElementAtOrDefault(2);
            Console.WriteLine(action switch { "estado" => networkPolicy.Status(), "agregar" => networkPolicy.Add(parts.ElementAtOrDefault(3) ?? ""), "listar" => networkPolicy.List(), "probar" => networkPolicy.Test(parts.ElementAtOrDefault(3) ?? ""), _ => "uso: /red whitelist estado|agregar|listar|probar" });
            return true;
        }

        if (trimmed.StartsWith("/usuarios ", StringComparison.OrdinalIgnoreCase)) { var p=trimmed.Split(' ',4,StringSplitOptions.RemoveEmptyEntries); Console.WriteLine(p.ElementAtOrDefault(1) switch {"listar"=>rbac.ListUsers(),"crear"=>rbac.CreateUser(p.ElementAtOrDefault(2)??"",p.ElementAtOrDefault(3)??"guest"),"eliminar"=>rbac.DeleteUser(p.ElementAtOrDefault(2)??""),"actual"=>rbac.SetCurrent(p.ElementAtOrDefault(2)??"local-root"),_=>"uso usuarios"}); return true; }
        if (trimmed.Equals("/roles", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(string.Join(Environment.NewLine, rbac.Roles)); return true; }
        if (trimmed.Equals("/permisos", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine("root/admin: all; senior_dev: code/nas read; junior_dev: memory/code; guest: status/help"); return true; }
        if (trimmed.StartsWith("/tenant ", StringComparison.OrdinalIgnoreCase)) { var p=trimmed.Split(' ',3,StringSplitOptions.RemoveEmptyEntries); Console.WriteLine(p.ElementAtOrDefault(1)=="crear"?rbac.CreateTenant(p.ElementAtOrDefault(2)??"default"):p.ElementAtOrDefault(1)=="listar"?rbac.ListTenants():"implemented: tenant local flat-file"); return true; }

        if (trimmed.StartsWith("/clamav", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(externalTools.ClamAvStatus()); return true; }
        if (trimmed.StartsWith("/docker", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(trimmed=="/docker estado"?externalTools.DockerStatus():externalTools.CommandDryRun("docker", trimmed)); return true; }
        if (trimmed.StartsWith("/nodered", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(externalTools.NodeRedStatus()); return true; }
        if (trimmed.StartsWith("/mqtt", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(externalTools.MqttStatus()); return true; }
        if (trimmed.StartsWith("/serverless", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(string.IsNullOrWhiteSpace(options.ServerlessWebhookUrl)?"missing_configuration: ServerlessWebhookUrl no configurado":options.DryRun?"dry_run: no se envió POST":"implemented: POST real configurado"); return true; }

        if (trimmed.StartsWith("/wol ", StringComparison.OrdinalIgnoreCase)) { var p=trimmed.Split(' ',3,StringSplitOptions.RemoveEmptyEntries); Console.WriteLine(p.ElementAtOrDefault(1) switch {"estado"=>$"{wol.Status().Status}: {wol.Status().Message}","probar"=>wol.IsValidMac(p.ElementAtOrDefault(2)??"")?"PASS MAC válida":"FAIL MAC inválida","enviar"=>wol.Send(p.ElementAtOrDefault(2)??"", confirm:false),"listar"=>"sin MACs configuradas","agregar"=>"partial: persistencia de alias MAC pendiente; validación disponible",_=>"uso wol"}); return true; }

        if (trimmed.StartsWith("/nas", StringComparison.OrdinalIgnoreCase)) { var p=trimmed.Split(' ',3,StringSplitOptions.RemoveEmptyEntries); if(p.ElementAtOrDefault(1)=="indexar") Console.WriteLine(await nas.IndexAsync(cancellationToken)); else Console.WriteLine(p.ElementAtOrDefault(1) switch {"estado"=>nas.Status(),"rutas"=>nas.Routes(),"buscar"=>nas.Search(p.ElementAtOrDefault(2)??""),"doctor"=>nas.Status(),_=>"uso nas"}); return true; }
        if (trimmed.StartsWith("/zeroleak ", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(zeroLeak.Sanitize(trimmed[10..])); return true; }
        if (trimmed.StartsWith("/intencion ", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("/enrutar ", StringComparison.OrdinalIgnoreCase)) { var text=trimmed[(trimmed.IndexOf(' ')+1)..]; var r=intentRouter.Route(text); Console.WriteLine($"intencion={r.intent} confianza={r.confidence:0.00} comando={r.command} requiere_confirmacion={r.requiresConfirmation}"); return true; }
        if (trimmed.StartsWith("/codigo traducir ", StringComparison.OrdinalIgnoreCase)) { var p=trimmed.Split(' ',5,StringSplitOptions.RemoveEmptyEntries); Console.WriteLine(translations.Create(p.ElementAtOrDefault(2)??"origen",p.ElementAtOrDefault(3)??"destino",p.ElementAtOrDefault(4)??"")); return true; }
        if (trimmed.Equals("/codigo traducciones", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(translations.List()); return true; }
        if (trimmed.StartsWith("/codigo traduccion estado ", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(translations.Status(trimmed.Split(' ').Last())); return true; }
        if (trimmed.Equals("/sistema doctor", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(systemDiagnostics.Doctor()); return true; }
        if (trimmed.Equals("/ntp estado", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(systemDiagnostics.Ntp()); return true; }
        if (trimmed.Equals("/ip estado", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(systemDiagnostics.Ip()); return true; }
        if (trimmed.Equals("/failsafe estado", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine("missing_hardware_or_network: configurar BIOS Restore on AC Power Loss manualmente"); return true; }
        if (trimmed.Equals("/voz estado", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(planners.Voice()); return true; }
        if (trimmed.Equals("/walkie estado", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(planners.Walkie()); return true; }
        if (trimmed.Equals("/visor estado", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(planners.RamViewer()); return true; }
        if (trimmed.Equals("/ingesta estado", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(planners.BlindIngest()); return true; }
        if (trimmed.Equals("/logs estado", StringComparison.OrdinalIgnoreCase) || trimmed.StartsWith("/logs limpiar", StringComparison.OrdinalIgnoreCase)) { Console.WriteLine(trimmed.Contains("--confirmar")?"implemented: limpieza confirmada no destructiva en esta fase":"dry_run: limpieza de logs requiere --confirmar"); return true; }

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

        if (trimmed.Equals("/auditoria verificar", StringComparison.OrdinalIgnoreCase))
        {
            var verification = auditLog.VerifyHashChain();
            Console.WriteLine($"{(verification.ok ? "PASS" : "FAIL")} {verification.message}");
            return true;
        }

        if (trimmed.Equals("/auditoria estado", StringComparison.OrdinalIgnoreCase))
        {
            var verification = auditLog.VerifyHashChain();
            Console.WriteLine($"audit_hash_chain={(verification.ok ? "implemented" : "failed")} entries={verification.entries} message={verification.message}");
            return true;
        }

        if (trimmed.Equals("/auditoria exportar", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine(rbac.Can("auditoria exportar") ? auditLog.ExportAudit() : "unsafe_without_confirmation: permiso requerido");
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
            "/summary", "/summary regenerar", "/indexar", "/indice estado", "/deps", "/vault estado", "/totp estado", "/red whitelist estado", "/usuarios listar", "/roles", "/tenant estado", "/clamav estado", "/docker estado", "/nodered estado", "/mqtt estado", "/wol estado", "/nas estado", "/zeroleak TEXTO", "/intencion TEXTO", "/serverless estado", "/sistema doctor", "/ntp estado", "/ip estado", "/failsafe estado", "/voz estado", "/walkie estado", "/visor estado", "/ingesta estado", "/logs estado", "/modulos", "/auditoria", "/auditoria verificar", "/auditoria estado", "/salir"
        })
        {
            Console.WriteLine($"- {command}");
        }
    }

    private static long GetSize(string path) => File.Exists(path) ? new FileInfo(path).Length : 0;

    private static string ReadSecret(string prompt)
    {
        Console.Write(prompt);
        return Console.ReadLine() ?? string.Empty;
    }
}
