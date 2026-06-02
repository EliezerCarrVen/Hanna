using Hanna.Lightweight.Core;
using Hanna.Lightweight.Services;

var options = LightweightOptions.CreateDefault();
var paths = new RuntimePaths(options.DataRoot);
var secretFilter = new SecretFilterService();
var jsonl = new JsonlStoreService(secretFilter);
var memory = new FlatFileMemoryService(paths, jsonl, secretFilter);
var markdown = new MarkdownVaultService(paths, secretFilter);
var search = new RipgrepSearchService(secretFilter);
var audit = new AuditLogService(paths, jsonl);
var modules = new ModuleRegistryService(search);
var codeCache = new CodeCacheService(paths, markdown, jsonl, secretFilter);
var startup = new LightweightStartupService(options, paths, memory, markdown, audit, modules, search);
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
Console.WriteLine("Comandos: /status, /memoria prueba, /memoria buscar TEXTO, /codigo prueba, /codigo buscar TEXTO, /modulos, /auditoria, /salir");

var router = new ConsoleCommandRouter(options, paths, memory, markdown, codeCache, search, modules, audit, startup);
while (true)
{
    Console.Write("> ");
    var command = Console.ReadLine();
    if (command is null || !await router.HandleAsync(command))
    {
        break;
    }
}
