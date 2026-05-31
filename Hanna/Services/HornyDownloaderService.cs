using Hanna.Core;

namespace Hanna.Services;

internal sealed class HornyDownloaderService
{
    private readonly AppConfig config;

    public HornyDownloaderService(AppConfig config)
    {
        this.config = config;
    }

    public string Help()
    {
        return
            "Comandos HD:\n" +
            "/hd - muestra ayuda HD.\n" +
            "/hd_status - muestra carpeta, logs y últimos videos detectados.\n" +
            "/hd_downloads - lista videos ya descargados por la app externa.\n" +
            "/hd_send N - manda por Telegram el video número N de la lista.\n" +
            "/hd_logs - muestra logs recientes.\n\n" +
            "Nota: HD en Hanna NO descarga. Solo detecta videos que ya estén en la carpeta de descargas y los manda por Telegram. Para descargar desde Hanna usa /d LINK.";
    }

    public string Status()
    {
        var sb = new StringBuilder();

        string downloads = GetDownloadsDir();
        string logs = GetLogsDir();

        sb.AppendLine("Estado HD:");
        sb.AppendLine($"Carpeta de descargas: {downloads}");
        sb.AppendLine($"Carpeta de logs: {logs}");
        sb.AppendLine();

        var files = GetRecentVideos(5);

        if (files.Count == 0)
        {
            sb.AppendLine("No encontré videos descargados en la carpeta configurada.");
        }
        else
        {
            sb.AppendLine("Últimos videos detectados:");
            for (int i = 0; i < files.Count; i++)
            {
                var info = new FileInfo(files[i]);
                sb.AppendLine($"{i + 1}. {Path.GetFileName(files[i])} - {Math.Round(info.Length / 1024.0 / 1024.0, 1)} MB");
            }
        }

        string logTail = ReadLatestLogTail(1200);

        if (!string.IsNullOrWhiteSpace(logTail))
        {
            sb.AppendLine();
            sb.AppendLine("Último log:");
            sb.AppendLine(logTail);
        }

        return sb.ToString().Trim();
    }

    public string ListDownloads()
    {
        var files = GetRecentVideos(20);

        if (files.Count == 0)
            return "No encontré videos descargados en la carpeta configurada.";

        var sb = new StringBuilder();
        sb.AppendLine("Videos disponibles para mandar por Telegram:");

        for (int i = 0; i < files.Count; i++)
        {
            var info = new FileInfo(files[i]);
            sb.AppendLine($"{i + 1}. {Path.GetFileName(files[i])} - {Math.Round(info.Length / 1024.0 / 1024.0, 1)} MB");
        }

        sb.AppendLine();
        sb.AppendLine("Para mandar uno usa /hd_send número. Ejemplo: /hd_send 1");

        return sb.ToString().Trim();
    }

    public string Logs()
    {
        string text = ReadLatestLogTail(3500);

        if (string.IsNullOrWhiteSpace(text))
            return "No encontré logs. Configura HORNY_LOGS_DIR si la app externa los guarda en otra carpeta.";

        return text;
    }

    public string? GetDownloadByIndex(int index, out string error)
    {
        error = "";
        var files = GetRecentVideos(20);

        if (index < 1 || index > files.Count)
        {
            error = "No encontré ese número en la lista. Usa /hd_downloads para ver los videos disponibles.";
            return null;
        }

        string file = files[index - 1];

        long maxBytes = GetTelegramMaxBytes();
        long size = new FileInfo(file).Length;

        if (size > maxBytes)
        {
            error = $"Ese video pesa {Math.Round(size / 1024.0 / 1024.0, 1)} MB y supera el límite configurado para Telegram.";
            return null;
        }

        return file;
    }

    public string DownloadDisabledMessage()
    {
        return
            "HD está configurado solo para mandar por Telegram videos ya descargados por la app externa.\n\n" +
            "Usa:\n" +
            "/hd_downloads\n" +
            "/hd_send 1\n\n" +
            "Si quieres descargar desde Hanna, usa /d LINK.";
    }

    private string GetDownloadsDir()
    {
        string raw = Environment.GetEnvironmentVariable("HORNY_DOWNLOADS_DIR") ?? "";

        if (!string.IsNullOrWhiteSpace(raw))
            return Environment.ExpandEnvironmentVariables(raw);

        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        string candidate = Path.Combine(documents, "horny-downloader", "downloads");

        if (Directory.Exists(candidate))
            return candidate;

        return Path.Combine(config.BaseDirectory, "horny_downloader_downloads");
    }

    private string GetLogsDir()
    {
        string raw = Environment.GetEnvironmentVariable("HORNY_LOGS_DIR") ?? "";

        if (!string.IsNullOrWhiteSpace(raw))
            return Environment.ExpandEnvironmentVariables(raw);

        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        return Path.Combine(documents, "horny-downloader", "logs");
    }

    private List<string> GetRecentVideos(int limit)
    {
        string dir = GetDownloadsDir();

        if (!Directory.Exists(dir))
            return new List<string>();

        return Directory.GetFiles(dir, "*", SearchOption.AllDirectories)
            .Where(IsVideoFile)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .Take(limit)
            .ToList();
    }

    private static bool IsVideoFile(string file)
    {
        return file.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
               file.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
               file.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
               file.EndsWith(".mov", StringComparison.OrdinalIgnoreCase);
    }

    private string ReadLatestLogTail(int maxChars)
    {
        string dir = GetLogsDir();

        if (!Directory.Exists(dir))
            return "";

        string? file = Directory.GetFiles(dir, "*.*", SearchOption.AllDirectories)
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(file) || !File.Exists(file))
            return "";

        try
        {
            string text = File.ReadAllText(file, Encoding.UTF8);

            if (text.Length > maxChars)
                text = text[^maxChars..];

            return text.Trim();
        }
        catch
        {
            return "";
        }
    }

    private static long GetTelegramMaxBytes()
    {
        string raw = Environment.GetEnvironmentVariable("TELEGRAM_MAX_VIDEO_MB") ?? "45";

        if (!long.TryParse(raw, out long mb))
            mb = 45;

        mb = Math.Clamp(mb, 1, 2000);

        return mb * 1024L * 1024L;
    }
}
