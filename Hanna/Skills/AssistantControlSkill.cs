using Hanna.Models;
using Hanna.Services;
using Hanna.Utilities;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class AssistantControlSkill : ISkill
{
    private readonly ModelModeService modelMode;
    private readonly ConfigUpdateService configUpdate;
    private readonly AppLauncherService appLauncher;
    private readonly WebcamLedService webcamLed;

    public AssistantControlSkill(
        ModelModeService modelMode,
        ConfigUpdateService configUpdate,
        AppLauncherService appLauncher,
        WebcamLedService webcamLed)
    {
        this.modelMode = modelMode;
        this.configUpdate = configUpdate;
        this.appLauncher = appLauncher;
        this.webcamLed = webcamLed;
    }

    public bool CanHandle(IntentResult intent)
    {
        return intent.Type is IntentType.EngineModeChange or IntentType.ConfigModify or IntentType.CameraControl or IntentType.Shutdown;
    }

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (intent.Type == IntentType.Shutdown)
            return SkillResult.Text(appLauncher.Shutdown(), true);

        if (intent.Type == IntentType.CameraControl)
            return SkillResult.Text(HandleCameraCommand(originalText), true);

        if (intent.Type == IntentType.EngineModeChange)
        {
            string normalizedEngineText = TextTools.Normalize(originalText);

            if (Regex.IsMatch(normalizedEngineText, @"^motores\b|\bmotores disponibles\b"))
            {
                return SkillResult.Text(
                    "Motores disponibles:\n" +
                    "- /motor ollama: usa Ollama local.\n" +
                    "- /motor gemini: usa Gemini directo como motor principal.\n" +
                    "- /motor groq: usa Groq directo como motor principal.\n" +
                    "- /motor openrouter: usa OpenRouter.\n" +
                    "- /motor hibrido: usa flujo híbrido.\n" +
                    "- /motor original: usa el flujo original.\n" +
                    "- /motor actual: muestra el motor activo.", true);
            }

            if (Regex.IsMatch(normalizedEngineText, @"^motor\s*(actual|estado)?$|\bmotor actual\b|\bque motor\b|\bqué motor\b"))
                return SkillResult.Text("Motor actual de Hanna: " + modelMode.GetModeLabel() + ".", true);

            EngineMode mode = ModelModeService.ParseFromText(originalText);
            await modelMode.SetMode(mode, cancellationToken);

            string explanation = mode switch
            {
                EngineMode.GroqOnly => "Motor Groq activado como motor principal. No usaré Gemini, OpenRouter ni Ollama salvo que lo pidas o habilites respaldo cruzado.",
                EngineMode.GeminiOnly => "Motor Gemini activado como motor principal. No usaré Groq, OpenRouter ni Ollama salvo que lo pidas o habilites respaldo cruzado.",
                EngineMode.Hybrid => "Motor híbrido activado. Usaré el flujo híbrido configurado para revisar y responder con más control.",
                EngineMode.OllamaLocal => "Motor Ollama local activado como motor principal. Responderé con el modelo local configurado.",
                EngineMode.OpenRouter => "Motor OpenRouter activado como motor principal. Usaré el modelo configurado en OpenRouter y registraré consumo de tokens.",
                _ => "Motor original activado. Hanna queda en el flujo original configurado."
            };

            return SkillResult.Text(explanation, true);
        }

        string addition = Regex.Replace(originalText, @"\b(cambia|modifica|actualiza|personalidad|configuracion|configuración|hanna)\b", " ", RegexOptions.IgnoreCase);
        addition = Regex.Replace(addition, @"\s+", " ").Trim();

        return SkillResult.Text(await configUpdate.AppendPersonality(addition, cancellationToken), true);
    }

    private string HandleCameraCommand(string text)
    {
        string normalized = TextTools.Normalize(text);

        if (Regex.IsMatch(normalized, @"\b(estado|como esta|cómo está|status)\b"))
        {
            string estadoCamara = webcamLed.IsCameraOpen ? "encendida" : "apagada";
            string estadoAuto = webcamLed.AutoIndicatorEnabled ? "activado" : "desactivado";
            return $"La cámara está {estadoCamara}. El indicador automático para voz está {estadoAuto}.";
        }

        if (Regex.IsMatch(normalized, @"\b(indicador|automatico|automático|cuando escuche|mientras escuchas)\b") &&
            Regex.IsMatch(normalized, @"\b(apaga|apagar|desactiva|desactivar|quita|quitar)\b"))
        {
            return webcamLed.SetAutoIndicator(false);
        }

        if (Regex.IsMatch(normalized, @"\b(indicador|automatico|automático|cuando escuche|mientras escuchas)\b") &&
            Regex.IsMatch(normalized, @"\b(enciende|encender|activa|activar|prende|prender)\b"))
        {
            return webcamLed.SetAutoIndicator(true);
        }

        if (Regex.IsMatch(normalized, @"\b(apaga|apagar|desactiva|desactivar|cierra|cerrar)\b"))
            return webcamLed.TurnOff();

        if (Regex.IsMatch(normalized, @"\b(enciende|encender|activa|activar|prende|prender|abre|abrir)\b"))
            return webcamLed.TurnOn();

        return webcamLed.Toggle();
    }
}

