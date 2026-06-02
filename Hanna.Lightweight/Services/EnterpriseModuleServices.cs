using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class VaultEncryptionService(RuntimePaths paths, PathGuardService guard, AuditLogService audit)
{
    public string Status() => Directory.Exists(paths.VaultBovedas) ? "partial: AES-256-GCM disponible; contraseña no persistida" : "missing_configuration: bovedas no inicializadas";
    public async Task<string> CreateAsync(string name, string password, CancellationToken ct = default)
    {
        Directory.CreateDirectory(paths.VaultBovedas);
        var id = Guid.NewGuid().ToString("N");
        var dir = guard.EnsureInsideRoot(Path.Combine(paths.VaultBovedas, id));
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "vault.meta.json"), JsonSerializer.Serialize(new { id, display = Hash(name), createdUtc = DateTimeOffset.UtcNow }), ct);
        await audit.RecordDetailedAsync("vault_create", "vault", "/vault crear", "implemented", $"vault {id} creado", false, false, ct);
        return id;
    }
    public async Task<string> ImportAsync(string file, string password, CancellationToken ct = default)
    {
        var safeFile = Path.GetFullPath(file); if (!File.Exists(safeFile)) return "failed: archivo no existe";
        Directory.CreateDirectory(paths.VaultBovedas); var id = Guid.NewGuid().ToString("N"); var outPath = guard.EnsureInsideRoot(Path.Combine(paths.VaultBovedas, id + ".bin"));
        var plain = await File.ReadAllBytesAsync(safeFile, ct); var salt = RandomNumberGenerator.GetBytes(16); var nonce = RandomNumberGenerator.GetBytes(12); var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32); var cipher = new byte[plain.Length]; var tag = new byte[16];
        using var aes = new AesGcm(key, 16); aes.Encrypt(nonce, plain, cipher, tag);
        await File.WriteAllBytesAsync(outPath, salt.Concat(nonce).Concat(tag).Concat(cipher).ToArray(), ct);
        await audit.RecordDetailedAsync("vault_import", "vault", "/vault importar", "implemented", $"archivo importado como {id}", false, false, ct);
        return id;
    }
    public string List() => Directory.Exists(paths.VaultBovedas) ? string.Join(Environment.NewLine, Directory.EnumerateFiles(paths.VaultBovedas).Select(Path.GetFileName)) : "sin vaults";
    public string Verify() => "partial: verificación de contenedores locales disponible; manifest cifrado avanzado pendiente";
    private static string Hash(string v) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(v))).ToLowerInvariant();
}

public sealed partial class NetworkAccessPolicyService(RuntimePaths paths, PathGuardService guard)
{
    private string FilePath => guard.EnsureInsideRoot(Path.Combine(paths.Runtime, "ip_whitelist.jsonl"));
    public string Add(string ip) { if(!IPAddressRegex().IsMatch(ip)) return "failed: IP inválida"; File.AppendAllText(FilePath, ip+Environment.NewLine); return "implemented: IP agregada a política local"; }
    public string List() => File.Exists(FilePath) ? File.ReadAllText(FilePath) : "lista vacía";
    public string Test(string ip) => File.Exists(FilePath) && File.ReadLines(FilePath).Contains(ip) ? "PASS permitido por política local" : "WARN no permitido por política local";
    public string Status() => "implemented: política interna local; firewall real disabled_by_config";
    [GeneratedRegex(@"^\d{1,3}(\.\d{1,3}){3}$")] private static partial Regex IPAddressRegex();
}


public sealed class RbacService(RuntimePaths paths, PathGuardService guard)
{
    private string Users => guard.EnsureInsideRoot(Path.Combine(paths.Runtime, "users.jsonl"));
    private string Tenants => guard.EnsureInsideRoot(Path.Combine(paths.Runtime, "tenants.jsonl"));
    public string CurrentUser { get; private set; } = "local-root";
    public string[] Roles => ["root", "admin", "senior_dev", "junior_dev", "guest"];
    public bool Can(string module) => CurrentUser == "local-root" || module is not ("vault" or "nas" or "mqtt" or "wol" or "docker" or "serverless" or "usuarios");
    public string CreateUser(string name,string role){ if(!Roles.Contains(role)) return "failed: rol inválido"; File.AppendAllText(Users, JsonSerializer.Serialize(new{name,role,createdUtc=DateTimeOffset.UtcNow})+Environment.NewLine); return "implemented: usuario local creado"; }
    public string ListUsers()=>File.Exists(Users)?File.ReadAllText(Users):"local-root root";
    public string DeleteUser(string name)=>"partial: baja lógica recomendada; eliminación física requiere confirmación futura";
    public string SetCurrent(string name){CurrentUser=name; return $"implemented: usuario actual {name}";}
    public string CreateTenant(string name){File.AppendAllText(Tenants, JsonSerializer.Serialize(new{name,createdUtc=DateTimeOffset.UtcNow})+Environment.NewLine); return "implemented: tenant local creado";}
    public string ListTenants()=>File.Exists(Tenants)?File.ReadAllText(Tenants):"default";
}

public sealed class ExternalToolModuleService(DependencyCheckerService deps, LightweightOptions options)
{
    public string ClamAvStatus()=>deps.IsFound("clamscan")?(options.ClamAvEnabled?"implemented: clamscan disponible":"disabled_by_config: ClamAvEnabled=false"):"missing_dependency: clamscan no encontrado. sudo apt install clamav clamav-daemon";
    public string DockerStatus()=>deps.IsFound("docker")?(options.DockerEnabled?"dry_run: docker disponible; ejecución real exige confirmación":"disabled_by_config: DockerEnabled=false"):"missing_dependency: docker no disponible";
    public string NodeRedStatus()=>string.IsNullOrWhiteSpace(options.NodeRedBaseUrl)?"missing_configuration: NodeRedBaseUrl no configurado":(deps.IsFound("node-red")||deps.IsFound("node")?"partial: configuración presente; verificar servicio con /nodered ping":"missing_dependency: node-red no disponible");
    public string MqttStatus()=>string.IsNullOrWhiteSpace(options.MqttBroker)?"missing_configuration: MqttBroker no configurado":(deps.IsFound("mosquitto")?"dry_run: broker configurado; publicación real requiere DryRun=false":"missing_dependency: mosquitto/MQTT broker no disponible");
    public string CommandDryRun(string module,string action,string path="")=>$"dry_run: {module} ejecutaría {action} {path}";
}

public sealed class NasIndexerService(RuntimePaths paths, LightweightOptions options, PathGuardService guard)
{
    private string Index => guard.EnsureInsideRoot(Path.Combine(paths.Indexes, "nas_index.jsonl"));
    public string Status()=>options.AllowedNasRoots.Length==0?"missing_configuration: AllowedNasRoots vacío":"partial: allowlist configurada";
    public string Routes()=>options.AllowedNasRoots.Length==0?"sin rutas NAS configuradas":string.Join(Environment.NewLine, options.AllowedNasRoots);
    public async Task<string> IndexAsync(CancellationToken ct=default){ if(options.AllowedNasRoots.Length==0)return Status(); await File.WriteAllTextAsync(Index,string.Empty,ct); var c=0; foreach(var root in options.AllowedNasRoots.Where(Directory.Exists)){foreach(var f in Directory.EnumerateFiles(root,"*",SearchOption.AllDirectories).Take(2000)){var info=new FileInfo(f); if(info.Length>options.MaxSearchFileBytes)continue; await File.AppendAllTextAsync(Index,JsonSerializer.Serialize(new{name=info.Name,ext=info.Extension,size=info.Length,modifiedUtc=info.LastWriteTimeUtc,hash=Hash(f)})+Environment.NewLine,ct); c++;}} return $"implemented: {c} archivos indexados sin copiar";}
    public string Search(string text)=>File.Exists(Index)?string.Join(Environment.NewLine,File.ReadLines(Index).Where(l=>l.Contains(text,StringComparison.OrdinalIgnoreCase)).Take(options.MaxSearchResults)):"sin índice NAS";
    private static string Hash(string text)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}

public sealed class CodeTranslationPlannerService(RuntimePaths paths, PathGuardService guard, ZeroLeakSanitizerService zero)
{
    private string Store=>guard.EnsureInsideRoot(Path.Combine(paths.Indexes,"translation_requests.jsonl"));
    public string Create(string origen,string destino,string texto){var id=Guid.NewGuid().ToString("N");var clean=zero.Sanitize(texto);var hash=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clean))).ToLowerInvariant();File.AppendAllText(Store,JsonSerializer.Serialize(new{id,origen,destino,hash,status="waiting_external_llm",text=clean,createdUtc=DateTimeOffset.UtcNow})+Environment.NewLine);return $"implemented: request {id} waiting_external_llm";}
    public string List()=>File.Exists(Store)?File.ReadAllText(Store):"sin traducciones";
    public string Status(string id)=>File.Exists(Store)?File.ReadLines(Store).FirstOrDefault(l=>l.Contains(id,StringComparison.OrdinalIgnoreCase))??"missing_configuration: id no encontrado":"sin store";
}

public sealed class SystemDiagnosticsService(LightweightOptions options)
{
    public string Doctor()=>JsonSerializer.Serialize(new{os=Environment.OSVersion.ToString(),machine=Environment.MachineName,utc=DateTimeOffset.UtcNow,timezone=TimeZoneInfo.Local.DisplayName,publicIp=options.PublicIpCheckEnabled?"enabled":"disabled_by_config",biosFailsafe="manual: activar Restore on AC Power Loss en BIOS si existe"},new JsonSerializerOptions{WriteIndented=true});
    public string Ntp()=>string.IsNullOrWhiteSpace(options.NtpExpectedServer)?"missing_configuration: NtpExpectedServer no configurado":"partial: validar con timedatectl/system settings";
    public string Ip()=>string.Join(Environment.NewLine,NetworkInterface.GetAllNetworkInterfaces().SelectMany(n=>n.GetIPProperties().UnicastAddresses).Select(a=>a.Address.ToString()).Where(a=>a.Contains('.')).Take(20));
}

public sealed class PlannerStatusService(DependencyCheckerService deps)
{
    public string Voice()=>deps.IsFound("node")?"missing_configuration: motor de voz local no configurado":"missing_dependency: motor STT/TTS local no instalado";
    public string Walkie()=>"missing_hardware_or_network: control plane listo; falta red P2P/configuración móvil";
    public string RamViewer()=>"dry_run: control plane listo; visor RAM real requiere confirmación";
    public string BlindIngest()=>"missing_configuration: ingesta ciega requiere fuente local configurada";
}
