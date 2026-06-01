using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using Hanna.Core;

namespace Hanna.Services;

internal sealed class TtsService
{
    private readonly AppConfig config;

    public TtsService(AppConfig config)
    {
        this.config = config;
    }

    public TtsService(AppConfig config, object? _)
        : this(config)
    {
    }

    public async Task<string?> Generate(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        string enabled = Environment.GetEnvironmentVariable("HANNA_TTS_ENABLED") ?? "true";
        string provider = Environment.GetEnvironmentVariable("TTS_PROVIDER") ?? "edge";

        if (enabled.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("none", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("off", StringComparison.OrdinalIgnoreCase) ||
            provider.Equals("disabled", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("[TTS] Desactivado por configuración.");
            return null;
        }

        if (!provider.Equals("edge", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"[TTS] Proveedor no soportado: {provider}. TTS omitido.");
            return null;
        }

        return await GenerateWithEdgeTts(text, cancellationToken);
    }

    private async Task<string?> GenerateWithEdgeTts(string text, CancellationToken cancellationToken)
    {
        string mp3Path = Path.Combine(Path.GetTempPath(), $"hanna_tts_{Guid.NewGuid():N}.mp3");
        string textPath = Path.Combine(Path.GetTempPath(), $"hanna_tts_{Guid.NewGuid():N}.txt");

        try
        {
            string voice = Environment.GetEnvironmentVariable("TTS_VOICE") ?? "es-PE-CamilaNeural";
            string rate = Environment.GetEnvironmentVariable("TTS_RATE") ?? "+0%";
            string pitch = Environment.GetEnvironmentVariable("TTS_PITCH") ?? "+0Hz";
            string volume = Environment.GetEnvironmentVariable("TTS_VOLUME") ?? "+0%";

            text = CleanTextForTts(text);

            if (string.IsNullOrWhiteSpace(text))
                return null;

            await File.WriteAllTextAsync(textPath, text, Encoding.UTF8, cancellationToken);

            string[][] attempts =
            {
                new[] { "edge-tts", "--voice", voice, $"--rate={rate}", $"--pitch={pitch}", $"--volume={volume}", "--file", textPath, "--write-media", mp3Path },
                new[] { "py", "-m", "edge_tts", "--voice", voice, $"--rate={rate}", $"--pitch={pitch}", $"--volume={volume}", "--file", textPath, "--write-media", mp3Path },
                new[] { "python", "-m", "edge_tts", "--voice", voice, $"--rate={rate}", $"--pitch={pitch}", $"--volume={volume}", "--file", textPath, "--write-media", mp3Path },
                new[] { "python3", "-m", "edge_tts", "--voice", voice, $"--rate={rate}", $"--pitch={pitch}", $"--volume={volume}", "--file", textPath, "--write-media", mp3Path }
            };

            foreach (string[] attempt in attempts)
            {
                string fileName = attempt[0];
                string[] args = attempt.Skip(1).ToArray();

                if (await TryRun(fileName, args, cancellationToken))
                    return ExistingNonEmpty(mp3Path);
            }

            Console.WriteLine("[TTS] No encontré edge-tts. Instálalo con: py -m pip install --user edge-tts");
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TTS Error]: {ex.Message}");
            return null;
        }
        finally
        {
            TryDelete(textPath);
        }
    }

    private static string? ExistingNonEmpty(string path)
    {
        if (File.Exists(path) && new FileInfo(path).Length > 0)
            return path;

        return null;
    }

    private static async Task<bool> TryRun(string fileName, IReadOnlyList<string> args, CancellationToken cancellationToken)
    {
        try
        {
            var psi = new ProcessStartInfo(fileName)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            foreach (string arg in args)
                psi.ArgumentList.Add(arg);

            using var process = new Process { StartInfo = psi };
            process.Start();

            string stdout = await process.StandardOutput.ReadToEndAsync(cancellationToken);
            string stderr = await process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode == 0)
                return true;

            if (!string.IsNullOrWhiteSpace(stderr))
                Console.WriteLine($"[TTS {fileName} Error]: {stderr.Trim()}");

            if (!string.IsNullOrWhiteSpace(stdout))
                Console.WriteLine($"[TTS {fileName} Output]: {stdout.Trim()}");

            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TTS {fileName} Error]: {ex.Message}");
            return false;
        }
    }

    private static string CleanTextForTts(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = Regex.Replace(text, @"```[\s\S]*?```", " código omitido ");
        text = Regex.Replace(text, @"https?://\S+", " enlace ");
        text = Regex.Replace(text, @"[/\\_`*#>\[\]{}|~]", " ");
        text = Regex.Replace(text, @"[\uD800-\uDBFF][\uDC00-\uDFFF]", " ");
        text = Regex.Replace(text, @"[\u2600-\u27BF]", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();

        if (text.Length > 900)
            text = text[..900] + "...";

        return text;
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
    }
}

