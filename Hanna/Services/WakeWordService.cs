using Hanna.Core;

namespace Hanna.Services;

internal sealed class WakeWordService : IDisposable
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService runtime;
    private readonly GroqService groq;
    private readonly VoiceCommandService voice;
    private CancellationTokenSource? cts;
    private Task? loopTask;

    public WakeWordService(AppConfig config, RuntimeSettingsService runtime, GroqService groq, VoiceCommandService voice)
    {
        this.config = config;
        this.runtime = runtime;
        this.groq = groq;
        this.voice = voice;
    }

    public void Start()
    {
        if (!(runtime.Snapshot().WakeWordEnabled || config.WakeWordEnabled))
        {
            Console.WriteLine("[Wake Word] Deshabilitada. Actívala desde el panel si quieres decir 'Hanna'.");
            return;
        }

        cts = new CancellationTokenSource();
        loopTask = Task.Run(() => Loop(cts.Token));
        Console.WriteLine("[Wake Word] Activa. Palabras: " + config.WakeWords);
    }

    private async Task Loop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (!runtime.Snapshot().WakeWordEnabled)
            {
                await Task.Delay(2000, token);
                continue;
            }

            try
            {
                using var recorder = new MicrophoneRecorderService();
                RuntimeSettings s = runtime.Snapshot();
                string? audio = await recorder.RecordUntilSilenceAsync(
                    silenceMs: 900,
                    noVoiceTimeoutMs: 7000,
                    maxSeconds: Math.Clamp(s.WakeWordListenSeconds, 2, 12),
                    minSeconds: 0,
                    voiceStartRms: Math.Max(100, s.LocalVoiceStartRms),
                    voiceStopRms: Math.Max(50, s.LocalVoiceStopRms),
                    cancellationToken: token,
                    recordImmediately: false,
                    initialGraceMs: 0);

                if (string.IsNullOrWhiteSpace(audio) || !File.Exists(audio))
                    continue;

                string text = await groq.TranscribeAudio(audio, token);
                TryDelete(audio);
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                string? command = ExtractCommandAfterWakeWord(text);
                if (!string.IsNullOrWhiteSpace(command))
                {
                    Console.WriteLine("[Wake Word] Activación: " + text.Trim());
                    await voice.HandleTextAsync(command, showOverlay: true, token);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Wake Word Error]: " + ex.Message);
                try { await Task.Delay(2000, token); } catch { }
            }
        }
    }

    private string? ExtractCommandAfterWakeWord(string text)
    {
        string normalized = Utilities.TextTools.Normalize(text);
        foreach (string wake in config.WakeWords.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string w = Utilities.TextTools.Normalize(wake);
            if (string.IsNullOrWhiteSpace(w)) continue;
            if (normalized.StartsWith(w + " ") || normalized == w)
            {
                string command = Regex.Replace(text, Regex.Escape(wake), "", RegexOptions.IgnoreCase).Trim(' ', ',', '.', ':', ';');
                return string.IsNullOrWhiteSpace(command) ? "Hanna" : command;
            }
        }
        return null;
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    public void Dispose()
    {
        try { cts?.Cancel(); } catch { }
        try { cts?.Dispose(); } catch { }
    }
}
