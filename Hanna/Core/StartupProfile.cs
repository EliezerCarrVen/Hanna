using Hanna.Models;
using Hanna.Services;

namespace Hanna.Core;

internal sealed class StartupProfile
{
    private readonly AppConfig config;
    private readonly RuntimeSettings settings;
    private readonly EngineMode activeEngine;
    private readonly bool webChatExplicitlyEnabled;

    private StartupProfile(AppConfig config, RuntimeSettings settings, EngineMode activeEngine, string mode)
    {
        this.config = config;
        this.settings = settings;
        this.activeEngine = activeEngine;
        Mode = mode;
        webChatExplicitlyEnabled = IsExplicitEnvTrue("HANNA_WEBCHAT_ENABLED");
    }

    public string Mode { get; }
    public bool IsFull => Mode == "full";
    public bool IsHybrid => Mode == "hybrid";
    public bool IsTelegramOnly => Mode == "telegram_only";

    public static StartupProfile Resolve(AppConfig config, RuntimeSettings settings, EngineMode activeEngine)
    {
        string requested = NormalizeMode(config.HannaMode);
        return new StartupProfile(config, settings, activeEngine, requested);
    }

    public StartupDecision DecideOllama()
    {
        if (config.OllamaAutoStart || activeEngine == EngineMode.OllamaLocal)
            return StartupDecision.Enabled("OLLAMA_AUTO_START=true o motor activo requiere Ollama.");

        return StartupDecision.Config("OLLAMA_AUTO_START=false y el motor activo no es Ollama.");
    }

    public StartupDecision DecideTelegram()
    {
        if (!IsValidTelegramToken(config.TelegramToken))
            return StartupDecision.Credentials("TELEGRAM_TOKEN vacío, placeholder o con formato inválido.");

        return StartupDecision.Enabled("TELEGRAM_TOKEN válido.");
    }

    public StartupDecision DecideAdminWeb()
    {
        if (IsTelegramOnly)
            return StartupDecision.Profile("HANNA_MODE=telegram_only no inicia Admin Web.");

        if (!config.AdminWebEnabled || !settings.AdminWebEnabled)
            return StartupDecision.Config("HANNA_ADMIN_WEB_ENABLED=false o desactivado en runtime_settings.json.");

        return StartupDecision.Enabled("Perfil permite Admin Web y HANNA_ADMIN_WEB_ENABLED=true.");
    }

    public StartupDecision DecideWebChat()
    {
        if (IsTelegramOnly)
            return StartupDecision.Profile("HANNA_MODE=telegram_only no inicia WebChat standalone.");

        if (!config.WebChatEnabled || !config.NodeJsEnabled)
            return StartupDecision.Config("HANNA_WEBCHAT_ENABLED=false o HANNA_NODEJS_ENABLED=false.");

        if (IsHybrid && !webChatExplicitlyEnabled)
            return StartupDecision.Profile("HANNA_MODE=hybrid desactiva WebChat standalone por defecto; usa HANNA_WEBCHAT_ENABLED=true para activarlo.");

        return StartupDecision.Enabled("Perfil permite WebChat standalone y HANNA_WEBCHAT_ENABLED=true.");
    }

    public StartupDecision DecideMobileApi()
    {
        if (IsTelegramOnly)
            return StartupDecision.Profile("HANNA_MODE=telegram_only no inicia Mobile API.");

        if (!config.MobileApiEnabled || !settings.MobileApiEnabled)
            return StartupDecision.Config("HANNA_MOBILE_API_ENABLED=false o desactivado en runtime_settings.json.");

        if (!IsValidTelegramToken(config.TelegramToken))
            return StartupDecision.Credentials("Mobile API requiere TelegramBotClient y TELEGRAM_TOKEN válido en la arquitectura actual.");

        return StartupDecision.Enabled("Perfil permite Mobile API y HANNA_MOBILE_API_ENABLED=true.");
    }

    public StartupDecision DecideHotkeys()
    {
        if (IsTelegramOnly)
            return StartupDecision.Profile("HANNA_MODE=telegram_only no inicia hotkeys locales.");

        if (!config.LocalHotkeyEnabled || !settings.LocalHotkeyEnabled)
            return StartupDecision.Config("HANNA_LOCAL_HOTKEY_ENABLED=false o desactivado en runtime_settings.json.");

        if (!IsValidTelegramToken(config.TelegramToken))
            return StartupDecision.Credentials("Las hotkeys locales usan TelegramBotClient en la arquitectura actual; falta TELEGRAM_TOKEN válido.");

        return StartupDecision.Enabled("Perfil permite hotkeys y HANNA_LOCAL_HOTKEY_ENABLED=true.");
    }

    public StartupDecision DecideOverlay()
    {
        if (IsTelegramOnly)
            return StartupDecision.Profile("HANNA_MODE=telegram_only no inicia overlay.");

        if (!config.OverlayEnabled || !settings.OverlayEnabled)
            return StartupDecision.Config("HANNA_OVERLAY_ENABLED=false o desactivado en runtime_settings.json.");

        return StartupDecision.Enabled("Perfil permite overlay y HANNA_OVERLAY_ENABLED=true.");
    }

    public StartupDecision DecideScreenAnalysis()
    {
        if (IsTelegramOnly)
            return StartupDecision.Profile("HANNA_MODE=telegram_only no inicia análisis de pantalla.");

        if (!config.ScreenAnalysisEnabled || !settings.ScreenAnalysisEnabled)
            return StartupDecision.Config("HANNA_SCREEN_ANALYSIS_ENABLED=false o desactivado en runtime_settings.json.");

        return StartupDecision.Enabled("Perfil permite análisis de pantalla y HANNA_SCREEN_ANALYSIS_ENABLED=true.");
    }

    public StartupDecision DecideWakeWord()
    {
        if (IsTelegramOnly)
            return StartupDecision.Profile("HANNA_MODE=telegram_only no inicia wake word.");

        if (!config.WakeWordEnabled && !settings.WakeWordEnabled)
            return StartupDecision.Config("HANNA_WAKE_WORD_ENABLED=false y wake word desactivada en runtime_settings.json.");

        if (string.IsNullOrWhiteSpace(config.GroqApiKey))
            return StartupDecision.Credentials("Wake word requiere GROQ_API_KEY para transcribir audio.");

        return StartupDecision.Enabled("Perfil permite wake word y HANNA_WAKE_WORD_ENABLED=true.");
    }

    public StartupDecision DecideAssignments()
    {
        if (IsTelegramOnly)
            return StartupDecision.Profile("HANNA_MODE=telegram_only no inicia tareas programadas.");

        if (!config.AssignmentsEnabled || !settings.AssignmentsEnabled)
            return StartupDecision.Config("HANNA_ASSIGNMENTS_ENABLED=false o desactivado en runtime_settings.json.");

        return StartupDecision.Enabled("Perfil permite tareas y HANNA_ASSIGNMENTS_ENABLED=true.");
    }

    public StartupDecision DecideNightlyMaintenance()
    {
        if (IsTelegramOnly)
            return StartupDecision.Profile("HANNA_MODE=telegram_only no inicia mantenimiento nocturno.");

        if (!config.NightlyMaintenanceEnabled)
            return StartupDecision.Config("HANNA_NIGHTLY_MAINTENANCE_ENABLED=false.");

        return StartupDecision.Enabled("Perfil permite mantenimiento y HANNA_NIGHTLY_MAINTENANCE_ENABLED=true.");
    }

    public StartupDecision DecideStartupLocalGreeting()
    {
        if (IsTelegramOnly)
            return StartupDecision.Profile("HANNA_MODE=telegram_only omite saludo local/TTS de arranque.");

        if (!settings.StartupGreetingEnabled)
            return StartupDecision.Config("HANNA_STARTUP_GREETING_ENABLED=false o desactivado en runtime_settings.json.");

        return StartupDecision.Enabled("Saludo local de arranque habilitado.");
    }

    public IReadOnlyList<(string Name, StartupDecision Decision)> BuildPlan()
    {
        return new List<(string, StartupDecision)>
        {
            ("Ollama", DecideOllama()),
            ("Telegram", DecideTelegram()),
            ("Admin Web", DecideAdminWeb()),
            ("WebChat standalone", DecideWebChat()),
            ("Mobile API", DecideMobileApi()),
            ("Hotkeys locales", DecideHotkeys()),
            ("Overlay", DecideOverlay()),
            ("Análisis de pantalla", DecideScreenAnalysis()),
            ("Wake word", DecideWakeWord()),
            ("Tareas", DecideAssignments()),
            ("Mantenimiento nocturno", DecideNightlyMaintenance()),
            ("Saludo local/TTS", DecideStartupLocalGreeting())
        };
    }

    public void PrintPlan()
    {
        Console.WriteLine($"[Arranque] Perfil activo HANNA_MODE={Mode}. Motor inicial: {activeEngine}.");
        foreach (var (name, decision) in BuildPlan())
        {
            string state = decision.Enabled ? "habilitado" : "omitido por " + decision.ReasonKind;
            Console.WriteLine($"[Arranque] {name}: {state}. {decision.Detail}");
        }
    }

    private static string NormalizeMode(string? value)
    {
        value = (value ?? "full").Trim().ToLowerInvariant().Replace('-', '_');
        return value switch
        {
            "full" => "full",
            "hybrid" or "hibrido" or "híbrido" => "hybrid",
            "telegram_only" or "telegramonly" or "telegram" or "telegram-only" => "telegram_only",
            _ => "full"
        };
    }

    private static bool IsExplicitEnvTrue(string key)
    {
        string? value = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("si", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("sí", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsValidTelegramToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return false;

        token = token.Trim();
        if (token.Contains("PEGA_AQUI", StringComparison.OrdinalIgnoreCase) ||
            token.Contains("TU_TOKEN", StringComparison.OrdinalIgnoreCase) ||
            token.Contains("TOKEN_REAL", StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = token.Split(':', 2);
        if (parts.Length != 2)
            return false;

        if (!long.TryParse(parts[0], out _))
            return false;

        if (parts[1].Length < 20)
            return false;

        foreach (char c in parts[1])
        {
            if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                return false;
        }

        return true;
    }
}

internal sealed record StartupDecision(bool Enabled, string ReasonKind, string Detail)
{
    public static StartupDecision Enabled(string detail) => new(true, "perfil/configuración", detail);
    public static StartupDecision Profile(string detail) => new(false, "perfil", detail);
    public static StartupDecision Config(string detail) => new(false, "configuración", detail);
    public static StartupDecision Credentials(string detail) => new(false, "credenciales", detail);
}
