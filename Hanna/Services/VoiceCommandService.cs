using Hanna.Core;
using Hanna.Models;
using Hanna.Skills;
using Telegram.Bot;

namespace Hanna.Services;

internal sealed class VoiceCommandService
{
    private readonly AppConfig config;
    private readonly MicrophoneRecorderService recorder;
    private readonly WebcamLedService webcamLed;
    private readonly GroqService groq;
    private readonly SkillRouter skillRouter;
    private readonly TelegramBotClient botClient;
    private readonly LocalAudioPlaybackService localAudio;
    private readonly ConversationLogService logs;
    private readonly MongoLogService mongoLogs;
    private readonly ModelModeService modelMode;
    private readonly TelegramMirrorService mirror;
    private readonly OverlayNotificationService overlay;
    private readonly CodeOutputService codeOutput;
    private readonly RuntimeSettingsService? runtime;
    private readonly ContextArchiveService? archive;
    private readonly SemaphoreSlim gate = new(1, 1);

    public VoiceCommandService(
        AppConfig config,
        MicrophoneRecorderService recorder,
        WebcamLedService webcamLed,
        GroqService groq,
        SkillRouter skillRouter,
        TelegramBotClient botClient,
        LocalAudioPlaybackService localAudio,
        ConversationLogService logs,
        MongoLogService mongoLogs,
        ModelModeService modelMode,
        TelegramMirrorService mirror,
        OverlayNotificationService overlay,
        CodeOutputService codeOutput,
        RuntimeSettingsService? runtime = null,
        ContextArchiveService? archive = null)
    {
        this.config = config;
        this.recorder = recorder;
        this.webcamLed = webcamLed;
        this.groq = groq;
        this.skillRouter = skillRouter;
        this.botClient = botClient;
        this.localAudio = localAudio;
        this.logs = logs;
        this.mongoLogs = mongoLogs;
        this.modelMode = modelMode;
        this.mirror = mirror;
        this.overlay = overlay;
        this.codeOutput = codeOutput;
        this.runtime = runtime;
        this.archive = archive;
    }

    public async Task ListenOnceAsync(bool showOverlay, CancellationToken cancellationToken)
    {
        if (!(runtime?.Snapshot().LocalHotkeyEnabled ?? config.LocalHotkeyEnabled))
        {
            Console.WriteLine("[Voz local] Hotkeys locales deshabilitadas desde panel web.");
            return;
        }

        if (!await gate.WaitAsync(0, cancellationToken))
        {
            Console.WriteLine("[Voz local] Ya estoy escuchando o procesando un comando.");
            if (showOverlay)
                await overlay.ShowAsync("Hanna", "Ya estoy escuchando o procesando un comando.", cancellationToken);
            return;
        }

        string? audioPath = null;

        try
        {
            Console.WriteLine("[Voz local] Escuchando. Habla ahora; cerraré cuando detecte silencio.");
            if (showOverlay)
                await overlay.ShowAsync("Hanna escucha", "Habla ahora. Me detengo sola cuando detecte silencio.", cancellationToken);

            TryBeep(950, 90);
            webcamLed.TurnOnForListeningIfEnabled();

            RuntimeSettings? live = runtime?.Snapshot();
            audioPath = await recorder.RecordUntilSilenceAsync(
                silenceMs: Math.Max(500, live?.LocalVoiceSilenceMs ?? config.LocalVoiceSilenceMs),
                noVoiceTimeoutMs: Math.Max(2000, live?.LocalVoiceNoVoiceTimeoutMs ?? config.LocalVoiceNoVoiceTimeoutMs),
                maxSeconds: Math.Max(5, live?.LocalVoiceMaxSeconds ?? config.LocalVoiceMaxSeconds),
                minSeconds: Math.Max(0, live?.LocalVoiceMinSeconds ?? config.LocalVoiceMinSeconds),
                voiceStartRms: Math.Max(100, live?.LocalVoiceStartRms ?? config.LocalVoiceStartRms),
                voiceStopRms: Math.Max(50, live?.LocalVoiceStopRms ?? config.LocalVoiceStopRms),
                cancellationToken: cancellationToken,
                recordImmediately: live?.VoiceRecordImmediately ?? config.VoiceRecordImmediately,
                initialGraceMs: live?.VoiceInitialGraceMs ?? config.VoiceInitialGraceMs);

            webcamLed.TurnOffAfterListeningIfEnabled();
            TryBeep(620, 90);

            if (string.IsNullOrWhiteSpace(audioPath) || !File.Exists(audioPath))
            {
                Console.WriteLine("[Voz local] No detecté voz clara.");
                if (showOverlay)
                    await overlay.ShowAsync("Hanna", "No detecté voz clara.", cancellationToken);
                return;
            }

            Console.WriteLine("[Voz local] Transcribiendo...");
            if (showOverlay)
                await overlay.ShowAsync("Hanna", "Transcribiendo...", cancellationToken);

            string text = await groq.TranscribeAudio(audioPath, cancellationToken);
            TryDelete(audioPath);

            if (string.IsNullOrWhiteSpace(text))
            {
                Console.WriteLine("[Voz local] No entendí el audio.");
                await localAudio.Speak("No entendí el audio.", cancellationToken);
                if (showOverlay)
                    await overlay.ShowAsync("Hanna", "No entendí el audio.", cancellationToken);
                return;
            }

            await HandleTextAsync(text, showOverlay, cancellationToken);
        }
        catch (Exception ex)
        {
            webcamLed.TurnOffAfterListeningIfEnabled();

            if (!string.IsNullOrWhiteSpace(audioPath))
                TryDelete(audioPath);

            Console.WriteLine($"[Voz local Error]: {ex}");
            await mirror.MirrorSystem("Error en voz local: " + ex.Message, CancellationToken.None);

            if (showOverlay)
                await overlay.ShowAsync("Hanna Error", ex.Message, CancellationToken.None);

            try
            {
                await localAudio.Speak("Tuve un error procesando el comando de voz local.", CancellationToken.None);
            }
            catch
            {
            }
        }
        finally
        {
            webcamLed.TurnOffAfterListeningIfEnabled();
            gate.Release();
        }
    }


    public async Task HandleTextAsync(string text, bool showOverlay, CancellationToken cancellationToken)
    {
        text = NormalizeWakeWord(text.Trim());
        long chatId = config.LocalChatId;

        Console.WriteLine($"👤 Usuario local → {text}");
        await logs.RegisterMessage(chatId, "USUARIO_LOCAL", text, cancellationToken);
        await mirror.MirrorLocalUser(text, cancellationToken);
        if (archive != null)
            await archive.SaveAsync(chatId, "local_voice", "usuario", text, cancellationToken);

        if (chatId != 0)
        {
            await mongoLogs.RegisterMessage(
                chatId,
                "USUARIO_LOCAL",
                "local_voice",
                text,
                "",
                "local",
                ShadowModeService.IsShadowActive(chatId),
                cancellationToken);
        }

        RuntimeSettings? currentLive = runtime?.Snapshot();
        using var temporaryMode = (currentLive?.PreferLocalForComputer ?? config.PreferLocalForComputer)
            ? modelMode.PushTemporaryMode(EngineMode.OllamaLocal)
            : null;

        SkillResult result = await skillRouter.Route(chatId, text, botClient, cancellationToken);

        if (result.SkipResponse)
            {
                Console.WriteLine("[Voz local] La skill pidió no responder.");
                return;
            }

            if (!result.Handled || string.IsNullOrWhiteSpace(result.ResponseText))
            {
                Console.WriteLine("[Voz local] Sin respuesta de skill. Usando fallback visible.");
                result = SkillResult.Text("Te escuché, pero Hanna no logró generar respuesta desde el motor. Revisa la línea [Router] en consola para ver qué intent se clasificó.");
            }

        Console.WriteLine($"🤖 Hanna local → {result.ResponseText}");
        await logs.RegisterMessage(chatId, "HANNA_LOCAL", result.ResponseText, cancellationToken);
        await mirror.MirrorLocalHanna(result.ResponseText, cancellationToken);
        if (archive != null)
            await archive.SaveAsync(chatId, "local_voice", "hanna", result.ResponseText, cancellationToken);

        string? generated = await codeOutput.SaveIfContainsCode(text, result.ResponseText, cancellationToken);
        if (!string.IsNullOrWhiteSpace(generated))
        {
            codeOutput.OpenGeneratedFile(generated);
            await mirror.MirrorSystem("Guardé código generado en: " + generated, cancellationToken);
        }

        if (showOverlay)
            await overlay.ShowAsync("Hanna", AppendFileInfo(result.ResponseText, generated), cancellationToken);

        if (chatId != 0)
        {
            await mongoLogs.RegisterMessage(
                chatId,
                "HANNA_LOCAL",
                "text",
                result.ResponseText,
                "",
                "local",
                ShadowModeService.IsShadowActive(chatId),
                cancellationToken);
        }

        await localAudio.Speak(result.ResponseText, cancellationToken);
    }

    private static string AppendFileInfo(string response, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return response;

        return response + "\n\nArchivo generado: " + path;
    }

    private static string NormalizeWakeWord(string text)
    {
        return Regex.Replace(text, @"\b(Gen[eé]|Jana|Ana)\b", "Hanna", RegexOptions.IgnoreCase);
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

    private static void TryBeep(int frequency, int duration)
    {
        try
        {
            Console.Beep(frequency, duration);
        }
        catch
        {
        }
    }
}

