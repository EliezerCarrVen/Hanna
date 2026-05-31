using Hanna.Core;
using System.Security.Cryptography;

namespace Hanna.Services;

internal sealed class NightlyMaintenanceService : IDisposable
{
    private readonly AppConfig config;
    private readonly TieredMemoryService memory;
    private readonly AuditTrailService audit;
    private CancellationTokenSource? cts;
    private Task? loopTask;

    public NightlyMaintenanceService(AppConfig config, TieredMemoryService memory, AuditTrailService audit)
    {
        this.config = config;
        this.memory = memory;
        this.audit = audit;
    }

    public void Start()
    {
        if (!config.NightlyMaintenanceEnabled)
        {
            Console.WriteLine("[Mantenimiento] Nocturno desactivado. Activa HANNA_NIGHTLY_MAINTENANCE_ENABLED=true para consolidar y respaldar automático.");
            return;
        }

        cts = new CancellationTokenSource();
        loopTask = Task.Run(() => Loop(cts.Token));
        Console.WriteLine("[Mantenimiento] Rutina nocturna activa. Hora objetivo: " + config.NightlyMaintenanceHour);
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            Directory.CreateDirectory(Path.Combine(config.TieredMemoryRoot, "daily"));
            Directory.CreateDirectory(Path.Combine(config.TieredMemoryRoot, "archive"));

            string collected = CollectRecentActivity();
            string summary = BuildSummary(collected);
            string dayFile = Path.Combine(config.TieredMemoryRoot, "daily", date + ".summary.md");
            await File.WriteAllTextAsync(dayFile, "# Resumen diario Hanna " + date + "\n\n" + summary + "\n", Encoding.UTF8, cancellationToken);
            string hash = Sha256(dayFile);
            string archive = await CompressIfPossible(dayFile, cancellationToken);
            string location = string.IsNullOrWhiteSpace(archive) ? "LOCAL" : "LOCAL_ARCHIVE";

            string finalArchive = string.IsNullOrWhiteSpace(archive) ? dayFile : archive;

            if (!string.IsNullOrWhiteSpace(config.BackupRemote) && !string.IsNullOrWhiteSpace(archive))
            {
                bool uploaded = await TryRcloneCopy(archive, config.BackupRemote, cancellationToken);
                if (uploaded)
                    location = "REMOTE";
            }

            await memory.UpsertDailySummaryAsync(date, summary, "hanna,daily,offline,maintenance", location, Path.GetRelativePath(config.BaseDirectory, finalArchive), hash, cancellationToken);
            await audit.AppendAsync(0, "NightlyMaintenance", "daily_consolidation", date + " => " + location, true, cancellationToken);
        }
        catch (Exception ex)
        {
            await audit.AppendAsync(0, "NightlyMaintenance", "daily_consolidation", ex.Message, false, cancellationToken);
            Console.WriteLine("[Mantenimiento Error]: " + ex.Message);
        }
    }

    private async Task Loop(CancellationToken cancellationToken)
    {
        string lastRunDate = "";
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                DateTime now = DateTime.Now;
                if (IsMaintenanceTime(now) && lastRunDate != now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
                {
                    await RunOnceAsync(cancellationToken);
                    lastRunDate = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
            }
            catch { }

            try { await Task.Delay(TimeSpan.FromMinutes(10), cancellationToken); } catch { }
        }
    }

    private bool IsMaintenanceTime(DateTime now)
    {
        if (!TimeSpan.TryParse(config.NightlyMaintenanceHour, CultureInfo.InvariantCulture, out TimeSpan target))
            target = new TimeSpan(3, 0, 0);
        TimeSpan diff = (now.TimeOfDay - target).Duration();
        return diff <= TimeSpan.FromMinutes(10);
    }

    private string CollectRecentActivity()
    {
        var sb = new StringBuilder();
        string[] directories =
        {
            config.LogsDirectory,
            config.ContextDirectory,
            config.ContextArchiveDirectory,
            Path.Combine(config.BaseDirectory, "registros_conversacion"),
            Path.Combine(config.BaseDirectory, "contexto_chats"),
            Path.Combine(config.BaseDirectory, "contexto_persistente")
        };

        DateTime cutoff = DateTime.Now.AddHours(-36);
        foreach (string dir in directories.Distinct())
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                continue;
            foreach (string file in Directory.EnumerateFiles(dir, "*.*", SearchOption.AllDirectories))
            {
                try
                {
                    var info = new FileInfo(file);
                    if (info.Length <= 0 || info.Length > 5_000_000 || info.LastWriteTime < cutoff)
                        continue;
                    string text = File.ReadAllText(file, Encoding.UTF8);
                    if (text.Length > 120000) text = text[^120000..];
                    sb.AppendLine("\n--- " + Path.GetRelativePath(config.BaseDirectory, file) + " ---");
                    sb.AppendLine(text);
                }
                catch { }
            }
        }
        return sb.ToString();
    }

    private static string BuildSummary(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "Sin actividad relevante detectada.";

        string[] keywords = { "error", "fall", "token", "motor", "spotify", "api", "mongo", "mysql", "ollama", "openrouter", "gemini", "groq", "hanna", "proyecto", "codigo", "código", "backup", "memoria", "fase", "netflix", "tv lg" };
        var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => x.Length > 0)
            .ToList();
        var important = lines.Where(l => keywords.Any(k => l.Contains(k, StringComparison.OrdinalIgnoreCase))).TakeLast(80).ToList();
        var chosen = important.Count > 0 ? important : lines.TakeLast(60).ToList();
        string result = string.Join(Environment.NewLine, chosen);
        return result.Length > 6000 ? result[^6000..] : result;
    }

    private static bool IsInternalLine(string line)
    {
        string value = line.ToLowerInvariant();
        string[] blocked =
        {
            "contexto modular de hanna",
            "hanna debe comportarse",
            "si el usuario dice",
            "siempre debes decir la verdad",
            "paso final de seguridad",
            "spotify_playlists",
            "modismos_mexicanos",
            "hannaenv",
            "telegram_token",
            "api_key",
            "jwt_secret",
            "pairing_token",
            "personalidad.txt",
            "reglas_verdad",
            "prompts_hanna",
            "instrucciones internas"
        };
        return blocked.Any(value.Contains);
    }

    private async Task<string> CompressIfPossible(string file, CancellationToken cancellationToken)
    {
        try
        {
            string archive = Path.Combine(config.TieredMemoryRoot, "archive", Path.GetFileName(file) + ".zst");
            var psi = new ProcessStartInfo(config.ZstdExecutable, $"-q -f \"{file}\" -o \"{archive}\"")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            using var process = Process.Start(psi);
            if (process == null) return "";
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0 && File.Exists(archive) ? archive : "";
        }
        catch
        {
            return "";
        }
    }

    private async Task<bool> TryRcloneCopy(string file, string remote, CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo(config.RcloneExecutable, $"copy \"{file}\" \"{remote}\" --transfers 1 --checkers 1 --bwlimit 500k")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            using var process = Process.Start(psi);
            if (process == null) return false;
            await process.WaitForExitAsync(cancellationToken);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string Sha256(string file)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(file);
        return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
    }

    public void Dispose()
    {
        try { cts?.Cancel(); } catch { }
        try { loopTask?.Wait(500); } catch { }
        cts?.Dispose();
    }
}
