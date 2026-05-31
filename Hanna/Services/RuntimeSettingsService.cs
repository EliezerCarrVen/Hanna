using Hanna.Core;

namespace Hanna.Services;

internal sealed class RuntimeSettingsService
{
    private readonly object sync = new();
    private readonly AppConfig config;
    private RuntimeSettings settings;

    public RuntimeSettingsService(AppConfig config)
    {
        this.config = config;
        settings = LoadFromDisk() ?? RuntimeSettings.FromConfig(config);
        Normalize(settings);
        ApplyToProcessEnvironment();
        Save();
    }

    private string RuntimePath => Path.Combine(config.SettingsDirectory, "runtime_settings.json");

    public RuntimeSettings Snapshot()
    {
        lock (sync)
            return settings.Clone();
    }

    public RuntimeSettings Update(Func<RuntimeSettings, RuntimeSettings> update)
    {
        lock (sync)
        {
            RuntimeSettings next = update(settings.Clone());
            Normalize(next);
            settings = next;
            SaveUnsafe();
            ApplyToProcessEnvironmentUnsafe();
            return settings.Clone();
        }
    }

    public RuntimeSettings Replace(RuntimeSettings next)
    {
        lock (sync)
        {
            Normalize(next);
            settings = next.Clone();
            SaveUnsafe();
            ApplyToProcessEnvironmentUnsafe();
            return settings.Clone();
        }
    }

    public void ApplyToProcessEnvironment()
    {
        lock (sync)
            ApplyToProcessEnvironmentUnsafe();
    }

    private RuntimeSettings? LoadFromDisk()
    {
        try
        {
            if (!File.Exists(RuntimePath))
                return null;

            string json = File.ReadAllText(RuntimePath, Encoding.UTF8);
            return JsonSerializer.Deserialize<RuntimeSettings>(json, JsonOptions());
        }
        catch
        {
            return null;
        }
    }

    public void Save()
    {
        lock (sync)
            SaveUnsafe();
    }

    private void SaveUnsafe()
    {
        Directory.CreateDirectory(config.SettingsDirectory);
        string json = JsonSerializer.Serialize(settings, JsonOptions());
        File.WriteAllText(RuntimePath, json, Encoding.UTF8);
    }

    private void ApplyToProcessEnvironmentUnsafe()
    {
        SetEnv("TTS_VOICE", settings.TtsVoice);
        SetEnv("TTS_FALLBACK_VOICE", settings.TtsFallbackVoice);
        SetEnv("TTS_RATE", settings.TtsRate);
        SetEnv("TTS_PITCH", settings.TtsPitch);
        SetEnv("TTS_VOLUME", settings.TtsVolume);
        SetEnv("OLLAMA_MODEL", settings.OllamaModel);
        SetEnv("OLLAMA_CONTEXT_SIZE", settings.OllamaContextSize.ToString(CultureInfo.InvariantCulture));
        SetEnv("HANNA_PROJECTS_DIRECTORY", settings.ProjectsDirectory);
        SetEnv("HANNA_AGENT_OUTPUT_DIRECTORY", settings.AgentOutputDirectory);
        SetEnv("HANNA_WAKE_WORD_ENABLED", settings.WakeWordEnabled.ToString());
        SetEnv("HANNA_DYNAMIC_SKILLS_ENABLED", settings.DynamicSkillsEnabled.ToString());
        SetEnv("HANNA_ASSIGNMENTS_ENABLED", settings.AssignmentsEnabled.ToString());
        SetEnv("HANNA_MOBILE_API_ENABLED", settings.MobileApiEnabled.ToString());
    }

    private static void SetEnv(string key, string value)
    {
        Environment.SetEnvironmentVariable(key, value ?? "");
    }

    private void Normalize(RuntimeSettings value)
    {
        value.OllamaBaseUrl = string.IsNullOrWhiteSpace(value.OllamaBaseUrl) ? config.OllamaBaseUrl : value.OllamaBaseUrl.Trim().TrimEnd('/');
        value.OllamaModel = string.IsNullOrWhiteSpace(value.OllamaModel) ? config.OllamaModel : value.OllamaModel.Trim();
        value.OllamaContextSize = Math.Clamp(value.OllamaContextSize <= 0 ? config.OllamaContextSize : value.OllamaContextSize, 1024, 16384);

        value.TtsVoice = string.IsNullOrWhiteSpace(value.TtsVoice) ? config.TtsVoice : value.TtsVoice.Trim();
        value.TtsFallbackVoice = string.IsNullOrWhiteSpace(value.TtsFallbackVoice) ? config.TtsFallbackVoice : value.TtsFallbackVoice.Trim();
        value.TtsRate = NormalizeTts(value.TtsRate, config.TtsRate);
        value.TtsPitch = NormalizePitch(value.TtsPitch, config.TtsPitch);
        value.TtsVolume = NormalizeTts(value.TtsVolume, config.TtsVolume);

        value.OverlaySeconds = Math.Clamp(value.OverlaySeconds <= 0 ? config.OverlaySeconds : value.OverlaySeconds, 3, 60);
        value.LocalVoiceSilenceMs = Math.Clamp(value.LocalVoiceSilenceMs <= 0 ? config.LocalVoiceSilenceMs : value.LocalVoiceSilenceMs, 500, 6000);
        value.LocalVoiceNoVoiceTimeoutMs = Math.Clamp(value.LocalVoiceNoVoiceTimeoutMs <= 0 ? config.LocalVoiceNoVoiceTimeoutMs : value.LocalVoiceNoVoiceTimeoutMs, 2000, 30000);
        value.LocalVoiceMaxSeconds = Math.Clamp(value.LocalVoiceMaxSeconds <= 0 ? config.LocalVoiceMaxSeconds : value.LocalVoiceMaxSeconds, 5, 120);
        value.LocalVoiceMinSeconds = Math.Clamp(value.LocalVoiceMinSeconds < 0 ? config.LocalVoiceMinSeconds : value.LocalVoiceMinSeconds, 0, 20);
        value.LocalVoiceStartRms = Math.Clamp(value.LocalVoiceStartRms <= 0 ? config.LocalVoiceStartRms : value.LocalVoiceStartRms, 50, 10000);
        value.LocalVoiceStopRms = Math.Clamp(value.LocalVoiceStopRms <= 0 ? config.LocalVoiceStopRms : value.LocalVoiceStopRms, 50, 10000);
        value.VoiceInitialGraceMs = Math.Clamp(value.VoiceInitialGraceMs <= 0 ? config.VoiceInitialGraceMs : value.VoiceInitialGraceMs, 100, 5000);
        value.WakeWordListenSeconds = Math.Clamp(value.WakeWordListenSeconds <= 0 ? config.WakeWordListenSeconds : value.WakeWordListenSeconds, 2, 12);
        value.AssignmentPollingMinutes = Math.Clamp(value.AssignmentPollingMinutes <= 0 ? config.AssignmentPollingMinutes : value.AssignmentPollingMinutes, 1, 120);
        value.MobileApiPort = Math.Clamp(value.MobileApiPort <= 0 ? config.MobileApiPort : value.MobileApiPort, 1024, 65535);

        value.ProjectIndexMaxFiles = Math.Clamp(value.ProjectIndexMaxFiles <= 0 ? config.ProjectIndexMaxFiles : value.ProjectIndexMaxFiles, 5, 500);
        value.ProjectIndexMaxChars = Math.Clamp(value.ProjectIndexMaxChars <= 0 ? config.ProjectIndexMaxChars : value.ProjectIndexMaxChars, 4000, 250000);

        value.ProjectsDirectory = NormalizeDirectory(value.ProjectsDirectory, config.ProjectsDirectory);
        value.AgentOutputDirectory = NormalizeDirectory(value.AgentOutputDirectory, config.AgentOutputDirectory);
        Directory.CreateDirectory(value.ProjectsDirectory);
        Directory.CreateDirectory(value.AgentOutputDirectory);
    }

    private static string NormalizeDirectory(string value, string fallback)
    {
        string path = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(path))
            return AppContext.BaseDirectory;

        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return string.IsNullOrWhiteSpace(fallback) ? AppContext.BaseDirectory : Path.GetFullPath(fallback);
        }
    }

    private static string NormalizeTts(string value, string fallback)
    {
        value = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return Regex.IsMatch(value, @"^[+-]?\d+%$") ? value : fallback;
    }

    private static string NormalizePitch(string value, string fallback)
    {
        value = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return Regex.IsMatch(value, @"^[+-]?\d+Hz$") ? value : fallback;
    }

    private static JsonSerializerOptions JsonOptions() => new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };
}

internal sealed class RuntimeSettings
{
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string OllamaModel { get; set; } = "qwen2.5-coder:3b";
    public int OllamaContextSize { get; set; } = 3072;
    public bool OllamaAutoStart { get; set; } = true;
    public string DefaultEngineMode { get; set; } = "ollama";
    public bool PreferLocalForComputer { get; set; } = true;
    public bool PreferHybridForTelegram { get; set; } = true;

    public bool MirrorLocalToTelegram { get; set; } = true;
    public bool OverlayEnabled { get; set; } = true;
    public int OverlaySeconds { get; set; } = 12;
    public bool StartupGreetingEnabled { get; set; } = true;
    public string StartupGreetingText { get; set; } = "Hanna está en línea. Modo local preparado.";

    public bool TtsEnabled { get; set; } = true;
    public string TtsProvider { get; set; } = "edge";
    public string TtsVoice { get; set; } = "es-PE-CamilaNeural";
    public string TtsFallbackVoice { get; set; } = "es-MX-DaliaNeural";
    public string TtsRate { get; set; } = "+4%";
    public string TtsPitch { get; set; } = "-2Hz";
    public string TtsVolume { get; set; } = "+1%";

    public bool LocalHotkeyEnabled { get; set; } = true;
    public bool LocalVoiceCameraLedEnabled { get; set; } = false;
    public int LocalVoiceSilenceMs { get; set; } = 1200;
    public int LocalVoiceNoVoiceTimeoutMs { get; set; } = 8000;
    public int LocalVoiceMaxSeconds { get; set; } = 20;
    public int LocalVoiceMinSeconds { get; set; } = 1;
    public int LocalVoiceStartRms { get; set; } = 900;
    public int LocalVoiceStopRms { get; set; } = 550;
    public bool VoiceRecordImmediately { get; set; } = true;
    public int VoiceInitialGraceMs { get; set; } = 900;
    public bool WakeWordEnabled { get; set; } = false;
    public string WakeWords { get; set; } = "Hanna,Oye Hanna,Hey Hanna,Hannah,Hana,Jana,Anna";
    public int WakeWordListenSeconds { get; set; } = 4;

    public string ProjectsDirectory { get; set; } = "";
    public string AgentOutputDirectory { get; set; } = "";
    public int ProjectIndexMaxFiles { get; set; } = 60;
    public int ProjectIndexMaxChars { get; set; } = 24000;
    public bool AgentOpenGeneratedCode { get; set; } = false;
    public bool ScreenAnalysisEnabled { get; set; } = true;

    public bool AdminWebEnabled { get; set; } = true;
    public int AdminWebPort { get; set; } = 8787;
    public bool AdminWebOpenBrowserOnStart { get; set; } = false;

    public bool DynamicSkillsEnabled { get; set; } = true;
    public bool AssignmentsEnabled { get; set; } = true;
    public int AssignmentPollingMinutes { get; set; } = 10;
    public bool GoogleIntegrationEnabled { get; set; } = false;
    public bool MobileApiEnabled { get; set; } = true;
    public int MobileApiPort { get; set; } = 8790;

    public RuntimeSettings Clone()
    {
        string json = JsonSerializer.Serialize(this);
        return JsonSerializer.Deserialize<RuntimeSettings>(json) ?? new RuntimeSettings();
    }

    public static RuntimeSettings FromConfig(AppConfig config)
    {
        return new RuntimeSettings
        {
            OllamaBaseUrl = config.OllamaBaseUrl,
            OllamaModel = config.OllamaModel,
            OllamaContextSize = config.OllamaContextSize,
            OllamaAutoStart = config.OllamaAutoStart,
            DefaultEngineMode = config.DefaultEngineMode,
            PreferLocalForComputer = config.PreferLocalForComputer,
            PreferHybridForTelegram = config.PreferHybridForTelegram,
            MirrorLocalToTelegram = config.MirrorLocalToTelegram,
            OverlayEnabled = config.OverlayEnabled,
            OverlaySeconds = config.OverlaySeconds,
            StartupGreetingEnabled = config.StartupGreetingEnabled,
            StartupGreetingText = config.StartupGreetingText,
            TtsVoice = config.TtsVoice,
            TtsFallbackVoice = config.TtsFallbackVoice,
            TtsRate = config.TtsRate,
            TtsPitch = config.TtsPitch,
            TtsVolume = config.TtsVolume,
            LocalHotkeyEnabled = config.LocalHotkeyEnabled,
            LocalVoiceCameraLedEnabled = config.LocalVoiceCameraLedEnabled,
            LocalVoiceSilenceMs = config.LocalVoiceSilenceMs,
            LocalVoiceNoVoiceTimeoutMs = config.LocalVoiceNoVoiceTimeoutMs,
            LocalVoiceMaxSeconds = config.LocalVoiceMaxSeconds,
            LocalVoiceMinSeconds = config.LocalVoiceMinSeconds,
            LocalVoiceStartRms = config.LocalVoiceStartRms,
            LocalVoiceStopRms = config.LocalVoiceStopRms,
            VoiceRecordImmediately = config.VoiceRecordImmediately,
            VoiceInitialGraceMs = config.VoiceInitialGraceMs,
            WakeWordEnabled = config.WakeWordEnabled,
            WakeWords = config.WakeWords,
            WakeWordListenSeconds = config.WakeWordListenSeconds,
            ProjectsDirectory = config.ProjectsDirectory,
            AgentOutputDirectory = config.AgentOutputDirectory,
            ProjectIndexMaxFiles = config.ProjectIndexMaxFiles,
            ProjectIndexMaxChars = config.ProjectIndexMaxChars,
            AgentOpenGeneratedCode = config.AgentOpenGeneratedCode,
            ScreenAnalysisEnabled = config.ScreenAnalysisEnabled,
            AdminWebEnabled = config.AdminWebEnabled,
            AdminWebPort = config.AdminWebPort,
            AdminWebOpenBrowserOnStart = config.AdminWebOpenBrowserOnStart,
            DynamicSkillsEnabled = config.DynamicSkillsEnabled,
            AssignmentsEnabled = config.AssignmentsEnabled,
            AssignmentPollingMinutes = config.AssignmentPollingMinutes,
            GoogleIntegrationEnabled = config.GoogleIntegrationEnabled,
            MobileApiEnabled = config.MobileApiEnabled,
            MobileApiPort = config.MobileApiPort
        };
    }
}
