using Hanna.Lightweight.Core;
using Hanna.Lightweight.Services;

var options = LightweightOptions.CreateDefault();
var paths = new RuntimePaths(options.DataRoot);
var pathGuard = new PathGuardService(paths);
var deps = new DependencyCheckerService();
var logRotation = new LogRotationService(options, paths, pathGuard);
var secretFilter = new SecretFilterService(paths, pathGuard, logRotation);
var jsonl = new JsonlStoreService(secretFilter, pathGuard, options, logRotation);
var memory = new FlatFileMemoryService(paths, jsonl, secretFilter, options);
var markdown = new MarkdownVaultService(paths, secretFilter, options, pathGuard);
var search = new RipgrepSearchService(secretFilter, options, pathGuard);
var audit = new AuditLogService(paths, jsonl, options, pathGuard);
var modules = new ModuleRegistryService(search, deps, options);
var startup = new LightweightStartupService(options, paths, pathGuard, logRotation, memory, markdown, audit, modules, search);
var codeCache = new CodeCacheService(paths, markdown, jsonl, secretFilter, pathGuard, options);
var doctor = new DoctorService(paths, pathGuard, search, modules, options, audit);
var summary = new RollingSummaryService(paths, memory, secretFilter, pathGuard, audit, options);
var vaultIndex = new VaultIndexService(paths, pathGuard, secretFilter, jsonl, audit, options);
var zeroLeak = new ZeroLeakSanitizerService(secretFilter);
var intentRouter = new IntentRouterService();
var wol = new WakeOnLanService(options);
var totp = new TotpService(paths, pathGuard);
var vaultCrypto = new VaultEncryptionService(paths, pathGuard, audit);
var networkPolicy = new NetworkAccessPolicyService(paths, pathGuard);
var rbac = new RbacService(paths, pathGuard);
var externalTools = new ExternalToolModuleService(deps, options);
var nas = new NasIndexerService(paths, options, pathGuard);
var translations = new CodeTranslationPlannerService(paths, pathGuard, zeroLeak);
var systemDiagnostics = new SystemDiagnosticsService(options);
var planners = new PlannerStatusService(deps);
var selfTest = new SelfTestService(paths, startup, memory, markdown, codeCache, search, audit, pathGuard, secretFilter, vaultCrypto, totp, rbac, nas, zeroLeak, intentRouter, wol, externalTools, summary, vaultIndex);
var router = new ConsoleCommandRouter(options, paths, memory, markdown, codeCache, search, modules, audit, startup, doctor, selfTest, summary, vaultIndex, deps, zeroLeak, intentRouter, wol, totp, vaultCrypto, networkPolicy, rbac, externalTools, nas, translations, systemDiagnostics, planners);

if (args.Any(arg => arg.Equals("--self-test", StringComparison.OrdinalIgnoreCase)))
{
    var results = await selfTest.RunAsync();
    PrintChecks(results);
    return DoctorService.GetGlobalStatus(results) == "FAIL" ? 1 : 0;
}

var onceIndex = Array.FindIndex(args, arg => arg.Equals("--once", StringComparison.OrdinalIgnoreCase));
if (onceIndex >= 0)
{
    await startup.StartAsync();
    var command = onceIndex + 1 < args.Length ? args[onceIndex + 1] : "/status";
    await router.HandleAsync(command);
    return 0;
}

var report = await startup.StartAsync();

Console.WriteLine("Hanna Lightweight iniciado");
Console.WriteLine($"modo: {report.Mode}");
Console.WriteLine($"memoria: {report.MemoryMode}");
Console.WriteLine($"vault path: {report.VaultPath}");
Console.WriteLine($"short memory path: {report.ShortMemoryPath}");
Console.WriteLine($"ripgrep {(report.RipgrepAvailable ? "disponible" : "no disponible")}");
Console.WriteLine("búnker cifrado: planificado, no implementado");
Console.WriteLine("MQTT: planificado, no implementado");
Console.WriteLine("Master/Worker: planificado, no implementado");
Console.WriteLine("NAS indexer: planificado, no implementado");
Console.WriteLine("Node-RED: planificado, no implementado");
Console.WriteLine("Wake-on-LAN: planificado, no implementado");
Console.WriteLine("Multi-tenant/RBAC: planificado, no implementado");
Console.WriteLine("Serverless: planificado, no implementado");
Console.WriteLine("Comandos: /help, /status, /doctor, /selftest, /memoria prueba, /memoria buscar TEXTO, /codigo prueba, /codigo buscar TEXTO, /codigo listar, /codigo estado, /summary, /summary regenerar, /indexar, /indice estado, /modulos, /auditoria, /salir");

while (true)
{
    Console.Write("> ");
    var command = Console.ReadLine();
    if (command is null || !await router.HandleAsync(command))
    {
        break;
    }
}

return 0;

static void PrintChecks(IReadOnlyList<CheckResult> checks)
{
    foreach (var check in checks)
    {
        Console.WriteLine($"{check.Status} {check.Name}: {check.Message}");
    }

    Console.WriteLine($"GLOBAL {DoctorService.GetGlobalStatus(checks)}");
}
