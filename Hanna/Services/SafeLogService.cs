using Hanna.Core;

namespace Hanna.Services;

internal sealed class SafeLogService
{
    private readonly AppConfig config;
    private readonly object sync = new();

    public SafeLogService(AppConfig config)
    {
        this.config = config;
    }

    private string LogsRoot => Path.Combine(config.BaseDirectory, "logs");

    public void Info(string channel, string message) => Write(channel, "INFO", message);
    public void Warning(string channel, string message) => Write(channel, "WARN", message);
    public void Error(string channel, Exception ex) => Write(channel, "ERROR", ex.ToString());
    public void Security(string message) => Write("security", "SECURITY", message);

    public void Write(string channel, string level, string message)
    {
        try
        {
            channel = NormalizeChannel(channel);
            Directory.CreateDirectory(LogsRoot);
            string path = Path.Combine(LogsRoot, channel + ".log");
            string line = $"{DateTimeOffset.Now:O} [{level}] {SecretSanitizer.Sanitize(message, 4000)}";
            lock (sync)
                File.AppendAllText(path, line + Environment.NewLine, Encoding.UTF8);

            if (level.Equals("ERROR", StringComparison.OrdinalIgnoreCase))
            {
                string errorPath = Path.Combine(LogsRoot, "errors.log");
                lock (sync)
                    File.AppendAllText(errorPath, line + Environment.NewLine, Encoding.UTF8);
            }
        }
        catch
        {
        }
    }

    public string BuildLogsSummary()
    {
        try
        {
            Directory.CreateDirectory(LogsRoot);
            string[] channels = { "telegram", "motores", "memoria", "audio", "security", "errors" };
            var sb = new StringBuilder();
            sb.AppendLine("Logs seguros de Hanna:");
            foreach (string channel in channels)
            {
                string path = Path.Combine(LogsRoot, channel + ".log");
                long bytes = File.Exists(path) ? new FileInfo(path).Length : 0;
                sb.AppendLine($"- {channel}.log: {(File.Exists(path) ? "existe" : "pendiente")}, {bytes:N0} bytes");
            }
            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            return "No pude leer el resumen de logs: " + SecretSanitizer.Sanitize(ex.Message);
        }
    }

    public string GetLastError()
    {
        try
        {
            string path = Path.Combine(LogsRoot, "errors.log");
            if (!File.Exists(path))
                return "No hay errores registrados en logs/errors.log.";

            string? last = File.ReadLines(path).LastOrDefault(x => !string.IsNullOrWhiteSpace(x));
            return string.IsNullOrWhiteSpace(last) ? "No hay errores registrados en logs/errors.log." : SecretSanitizer.Sanitize(last, 1200);
        }
        catch (Exception ex)
        {
            return "No pude leer el último error: " + SecretSanitizer.Sanitize(ex.Message);
        }
    }

    private static string NormalizeChannel(string channel)
    {
        channel = (channel ?? "errors").Trim().ToLowerInvariant();
        return channel switch
        {
            "telegram" => "telegram",
            "motores" or "motors" or "engine" => "motores",
            "memoria" or "memory" => "memoria",
            "audio" => "audio",
            "security" or "seguridad" => "security",
            "errors" or "errores" or "error" => "errors",
            _ => "errors"
        };
    }
}
