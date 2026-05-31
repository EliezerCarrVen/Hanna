using Hanna.Core;

namespace Hanna.Services;

internal sealed class WebVideoDownloadService
{
    private readonly AppConfig config;

    public WebVideoDownloadService(AppConfig config)
    {
        this.config = config;
    }

    public async Task<WebVideoDownloadResult> DownloadVideo(string originalText, long chatId, CancellationToken cancellationToken)
    {
        string url = ExtractUrl(originalText);

        if (string.IsNullOrWhiteSpace(url))
            return WebVideoDownloadResult.Fail("Mándame el enlace después del comando. Ejemplo: /d https://pagina.com/video");

        string downloadDir = Path.Combine(config.BaseDirectory, "videos_extraidos");
        Directory.CreateDirectory(downloadDir);

        string before = DateTime.UtcNow.ToString("O");

        var pullResult = await TryPullVids(url, downloadDir, chatId, cancellationToken);

        if (pullResult.Success)
            return pullResult;

        var ytDlpResult = await TryYtDlp(url, downloadDir, chatId, cancellationToken);

        if (ytDlpResult.Success)
            return ytDlpResult;

        var customResult = await TryCustomDownloader(url, downloadDir, chatId, cancellationToken);

        if (customResult.Success)
            return customResult;

        string details =
            "No pude extraer ese video con pull-vids ni yt-dlp. " +
            "Puede que el sitio no sea compatible, que requiera sesión, que el video sea privado o que tenga protección. " +
            "Detalle pull-vids: " + pullResult.Message + " | yt-dlp: " + ytDlpResult.Message;

        return WebVideoDownloadResult.Fail(details);
    }

    private async Task<WebVideoDownloadResult> TryPullVids(string url, string downloadDir, long chatId, CancellationToken cancellationToken)
    {
        string pullVidsPath = Environment.GetEnvironmentVariable("PULL_VIDS_PATH") ?? "pull-vids";

        string markerPrefix = $"pullvids_{chatId}_{DateTime.Now:yyyyMMdd_HHmmss}";
        string markerDir = Path.Combine(downloadDir, markerPrefix);
        Directory.CreateDirectory(markerDir);

        var psi = new ProcessStartInfo(pullVidsPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(markerDir);
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("mp4");
        psi.ArgumentList.Add("--no-banner");
        psi.ArgumentList.Add(url);

        var result = await RunProcess(psi, cancellationToken);

        if (!result.Success)
            return WebVideoDownloadResult.Fail(result.Error);

        string? file = FindNewestVideo(markerDir);

        if (string.IsNullOrWhiteSpace(file))
            return WebVideoDownloadResult.Fail("pull-vids terminó, pero no encontré video generado.");

        return ValidateForTelegram(file);
    }

    private async Task<WebVideoDownloadResult> TryYtDlp(string url, string downloadDir, long chatId, CancellationToken cancellationToken)
    {
        string ytDlpPath = Environment.GetEnvironmentVariable("YTDLP_PATH") ?? "yt-dlp";
        string outputTemplate = Path.Combine(downloadDir, $"ytdlp_{chatId}_{DateTime.Now:yyyyMMdd_HHmmss}.%(ext)s");

        var psi = new ProcessStartInfo(ytDlpPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true
        };

        psi.ArgumentList.Add("--no-playlist");
        psi.ArgumentList.Add("--restrict-filenames");
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("bv*[ext=mp4]+ba[ext=m4a]/b[ext=mp4]/best");
        psi.ArgumentList.Add("--merge-output-format");
        psi.ArgumentList.Add("mp4");
        psi.ArgumentList.Add("-o");
        psi.ArgumentList.Add(outputTemplate);
        psi.ArgumentList.Add(url);

        var result = await RunProcess(psi, cancellationToken);

        if (!result.Success)
            return WebVideoDownloadResult.Fail(result.Error);

        string? file = FindNewestVideo(downloadDir, $"ytdlp_{chatId}_");

        if (string.IsNullOrWhiteSpace(file))
            return WebVideoDownloadResult.Fail("yt-dlp terminó, pero no encontré video generado.");

        return ValidateForTelegram(file);
    }

    private async Task<WebVideoDownloadResult> TryCustomDownloader(string url, string downloadDir, long chatId, CancellationToken cancellationToken)
    {
        string commandTemplate = Environment.GetEnvironmentVariable("HORNY_DOWNLOADER_COMMAND") ?? "";

        if (string.IsNullOrWhiteSpace(commandTemplate))
            return WebVideoDownloadResult.Fail("HORNY_DOWNLOADER_COMMAND no configurado.");

        string targetDir = Path.Combine(downloadDir, $"custom_{chatId}_{DateTime.Now:yyyyMMdd_HHmmss}");
        Directory.CreateDirectory(targetDir);

        string commandLine = commandTemplate
            .Replace("{url}", url)
            .Replace("{output}", targetDir)
            .Replace("{dir}", targetDir);

        var psi = BuildShellProcess(commandLine);

        var result = await RunProcess(psi, cancellationToken);

        if (!result.Success)
            return WebVideoDownloadResult.Fail(result.Error);

        string? file = FindNewestVideo(targetDir);

        if (string.IsNullOrWhiteSpace(file))
            file = FindNewestVideo(downloadDir);

        if (string.IsNullOrWhiteSpace(file))
            return WebVideoDownloadResult.Fail("El downloader personalizado terminó, pero no encontré video generado.");

        return ValidateForTelegram(file);
    }

    private static ProcessStartInfo BuildShellProcess(string commandLine)
    {
        if (OperatingSystem.IsWindows())
        {
            var psi = new ProcessStartInfo("cmd.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            psi.ArgumentList.Add("/c");
            psi.ArgumentList.Add(commandLine);
            return psi;
        }
        else
        {
            var psi = new ProcessStartInfo("/bin/bash")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            psi.ArgumentList.Add("-lc");
            psi.ArgumentList.Add(commandLine);
            return psi;
        }
    }

    private static async Task<ProcessRunResult> RunProcess(ProcessStartInfo psi, CancellationToken cancellationToken)
    {
        try
        {
            using var process = new Process { StartInfo = psi };

            process.Start();

            string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            string stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode != 0)
            {
                string detail = string.IsNullOrWhiteSpace(stderr) ? stdout : stderr;
                return new ProcessRunResult(false, Utilities.TextTools.Clip(detail, 800));
            }

            return new ProcessRunResult(true, stdout);
        }
        catch (Exception ex)
        {
            return new ProcessRunResult(false, ex.Message);
        }
    }

    private static string? FindNewestVideo(string directory, string prefix = "")
    {
        if (!Directory.Exists(directory))
            return null;

        var files = Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
            .Where(f =>
                (string.IsNullOrWhiteSpace(prefix) || Path.GetFileName(f).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) &&
                (
                    f.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".mkv", StringComparison.OrdinalIgnoreCase) ||
                    f.EndsWith(".mov", StringComparison.OrdinalIgnoreCase)
                ))
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToList();

        return files.FirstOrDefault();
    }

    private static WebVideoDownloadResult ValidateForTelegram(string file)
    {
        if (!File.Exists(file))
            return WebVideoDownloadResult.Fail("No encontré el archivo generado.");

        long maxBytes = GetTelegramMaxBytes();
        var info = new FileInfo(file);

        if (info.Length > maxBytes)
        {
            return WebVideoDownloadResult.Fail(
                $"El video se descargó, pero pesa {Math.Round(info.Length / 1024.0 / 1024.0, 1)} MB. Archivo local: {file}");
        }

        return WebVideoDownloadResult.Ok(file);
    }

    private static string ExtractUrl(string text)
    {
        var match = Regex.Match(text, @"https?://\S+", RegexOptions.IgnoreCase);

        if (!match.Success)
            return "";

        return match.Value.Trim().TrimEnd('.', ',', ';', ')', ']');
    }

    private static long GetTelegramMaxBytes()
    {
        string raw = Environment.GetEnvironmentVariable("TELEGRAM_MAX_VIDEO_MB") ?? "45";

        if (!long.TryParse(raw, out long mb))
            mb = 45;

        mb = Math.Clamp(mb, 1, 2000);

        return mb * 1024L * 1024L;
    }

    private sealed record ProcessRunResult(bool Success, string Error);
}

internal sealed record WebVideoDownloadResult(bool Success, string FilePath, string Message)
{
    public static WebVideoDownloadResult Ok(string filePath) => new(true, filePath, "");
    public static WebVideoDownloadResult Fail(string message) => new(false, "", message);
}
