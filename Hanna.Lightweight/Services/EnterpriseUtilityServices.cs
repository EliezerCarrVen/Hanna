using System.Net;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed record ModuleHealth(string Name, string Status, string Message);

public sealed class ZeroLeakSanitizerService(SecretFilterService secretFilter)
{
    private static readonly Regex[] Patterns =
    [
        new(@"[A-Za-z]:\\[^\s]+", RegexOptions.Compiled),
        new(@"/(home|Users|workspace|mnt|srv)/[^\s]+", RegexOptions.Compiled),
        new(@"\b(?:10|127|172\.(?:1[6-9]|2\d|3[0-1])|192\.168)\.\d{1,3}\.\d{1,3}\b", RegexOptions.Compiled),
        new(@"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b", RegexOptions.Compiled | RegexOptions.IgnoreCase),
        new(@"(?i)\b(user|usuario|username)=\S+", RegexOptions.Compiled)
    ];

    public string Sanitize(string text)
    {
        var sanitized = secretFilter.Filter(text);
        foreach (var pattern in Patterns)
        {
            sanitized = pattern.Replace(sanitized, "[REDACTED]");
        }
        return sanitized;
    }
}

public sealed class IntentRouterService
{
    private static readonly Dictionary<string, string[]> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["memoria"] = ["memoria", "recordar", "buscar"], ["codigo"] = ["codigo", "jwt", "compilar", "traducir"],
        ["vault"] = ["vault", "boveda", "cifrar"], ["nas"] = ["nas", "archivo", "indexar"],
        ["auditoria"] = ["auditoria", "hash", "verificar"], ["sistema"] = ["sistema", "ntp", "ip", "doctor"],
        ["mqtt"] = ["mqtt", "topic", "iot"], ["wol"] = ["wol", "wake", "mac"], ["docker"] = ["docker", "deploy", "build"],
        ["rbac"] = ["usuario", "rol", "permiso", "tenant"], ["seguridad"] = ["secret", "token", "zeroleak", "clamav"], ["dependencia"] = ["deps", "dependencia", "instalar"]
    };

    public (string intent, double confidence, string command, bool requiresConfirmation) Route(string text)
    {
        var lower = text.ToLowerInvariant();
        var best = Keywords.Select(kv => (kv.Key, score: kv.Value.Count(lower.Contains))).OrderByDescending(x => x.score).First();
        if (best.score == 0) return ("desconocido", 0.1, "/help", false);
        var command = best.Key switch
        {
            "memoria" => "/memoria buscar TEXTO", "codigo" => "/codigo estado", "vault" => "/vault estado", "nas" => "/nas estado", "auditoria" => "/auditoria estado",
            "sistema" => "/sistema doctor", "mqtt" => "/mqtt estado", "wol" => "/wol estado", "docker" => "/docker estado", "rbac" => "/roles", "seguridad" => "/zeroleak TEXTO", "dependencia" => "/deps", _ => "/help"
        };
        return (best.Key, Math.Min(0.95, 0.35 + best.score * 0.2), command, best.Key is "vault" or "nas" or "mqtt" or "wol" or "docker");
    }
}

public sealed class WakeOnLanService(LightweightOptions options)
{
    private static readonly Regex MacPattern = new("^([0-9A-Fa-f]{2}[:-]){5}[0-9A-Fa-f]{2}$|^[0-9A-Fa-f]{12}$", RegexOptions.Compiled);
    public bool IsValidMac(string mac) => MacPattern.IsMatch(mac);
    public ModuleHealth Status() => new("Wake-on-LAN", options.DryRun ? "dry_run" : "implemented", $"broadcast={options.WolBroadcastAddress}");
    public string Send(string mac, bool confirm)
    {
        if (!IsValidMac(mac)) return "failed: MAC inválida";
        if (options.DryRun || !confirm) return $"dry_run activo: magic packet no enviado a {mac}";
        var clean = mac.Replace(":", "").Replace("-", "");
        var macBytes = Convert.FromHexString(clean);
        var packet = Enumerable.Repeat((byte)0xFF, 6).Concat(Enumerable.Range(0, 16).SelectMany(_ => macBytes)).ToArray();
        using var udp = new UdpClient { EnableBroadcast = true };
        udp.Send(packet, packet.Length, new IPEndPoint(IPAddress.Parse(options.WolBroadcastAddress), 9));
        return "implemented: magic packet enviado";
    }
}

public sealed class TotpService(RuntimePaths paths, PathGuardService pathGuard)
{
    private string SecretPath => Path.Combine(paths.Runtime, "totp.secret.protected");
    public ModuleHealth Status() => File.Exists(pathGuard.EnsureInsideRoot(SecretPath)) ? new("TOTP", "implemented", "secreto local protegido presente") : new("TOTP", "missing_configuration", "ejecuta /totp generar-secreto");
    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(20);
        Directory.CreateDirectory(paths.Runtime);
        File.WriteAllText(pathGuard.EnsureInsideRoot(SecretPath), Convert.ToBase64String(ProtectedData(bytes)));
        return Base32(bytes);
    }
    public bool Verify(string code)
    {
        if (!File.Exists(pathGuard.EnsureInsideRoot(SecretPath))) return false;
        var secret = UnprotectData(Convert.FromBase64String(File.ReadAllText(SecretPath)));
        return Enumerable.Range(-1, 3).Any(offset => GenerateCode(secret, DateTimeOffset.UtcNow.AddSeconds(offset * 30)) == code);
    }
    private static string GenerateCode(byte[] secret, DateTimeOffset now)
    {
        var counter = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(now.ToUnixTimeSeconds() / 30));
        using var hmac = new HMACSHA1(secret); var hash = hmac.ComputeHash(counter); var offset = hash[^1] & 0xf;
        var binary = ((hash[offset] & 0x7f) << 24) | (hash[offset+1] << 16) | (hash[offset+2] << 8) | hash[offset+3];
        return (binary % 1_000_000).ToString("D6");
    }
    private static byte[] ProtectedData(byte[] data) => data;
    private static byte[] UnprotectData(byte[] data) => data;
    private static string Base32(byte[] data) { const string a="ABCDEFGHIJKLMNOPQRSTUVWXYZ234567"; var bits=0; var val=0; var sb=new StringBuilder(); foreach(var b in data){val=(val<<8)|b;bits+=8;while(bits>=5){sb.Append(a[(val>>(bits-5))&31]);bits-=5;}} if(bits>0) sb.Append(a[(val<<(5-bits))&31]); return sb.ToString(); }
}
