namespace Hanna.Core;

public sealed class AppConfig
{
    public string BaseDirectory { get; init; } = "";
    public string EnvPath { get; init; } = "";
    public string PersonalityPath { get; init; } = "";
    public string TelegramToken { get; init; } = "";
    public string GroqApiKey { get; init; } = "";
    public string GroqChatModel { get; init; } = "llama-3.3-70b-versatile";
    public string GroqVisionModel { get; init; } = "meta-llama/llama-4-scout-17b-16e-instruct";
    public string GeminiApiKey { get; init; } = "";
    public string GeminiModel { get; init; } = "gemini-2.5-flash";
    public bool GeminiWebVerify { get; init; } = true;
    public string OllamaBaseUrl { get; init; } = "http://localhost:11434";
    public string OllamaModel { get; init; } = "qwen2.5-coder:3b";
    public int OllamaContextSize { get; init; } = 4096;
    public bool OllamaAutoStart { get; init; } = true;
    public string OllamaExecutable { get; init; } = "ollama";
    public int OllamaStartupTimeoutSeconds { get; init; } = 25;
    public string OpenRouterApiKey { get; init; } = "";
    public string OpenRouterBaseUrl { get; init; } = "https://openrouter.ai/api/v1/chat/completions";
    public string OpenRouterDefaultModel { get; init; } = "openai/gpt-4o-mini";
    public string OpenRouterArchitectModel { get; init; } = "anthropic/claude-3.5-opus";
    public string OpenRouterEngineerModel { get; init; } = "anthropic/claude-3.5-sonnet";
    public string OpenRouterOperatorModel { get; init; } = "google/gemini-2.0-flash";
    public string OpenRouterAnalystModel { get; init; } = "meta-llama/llama-3.1-8b-instruct";
    public string OpenRouterCreativeModel { get; init; } = "openai/gpt-4o-mini";
    public string OpenRouterAppTitle { get; init; } = "Hanna Astro Soluciones";
    public string OpenRouterReferer { get; init; } = "http://localhost";
    public decimal OpenRouterDailyBudgetUsd { get; init; } = 1.00m;
    public decimal OpenRouterEstimatedUsdPer1KTokens { get; init; } = 0.001m;
    public decimal OpenRouterArchitectUsdPer1KTokens { get; init; } = 0.015m;
    public decimal OpenRouterEngineerUsdPer1KTokens { get; init; } = 0.003m;
    public decimal OpenRouterOperatorUsdPer1KTokens { get; init; } = 0.0005m;
    public decimal OpenRouterAnalystUsdPer1KTokens { get; init; } = 0.0000m;
    public string TokenUsageDirectory { get; init; } = "";
    public int TokenUploadWarningThreshold { get; init; } = 8000;
    public long MaxFileTokenPreviewBytes { get; init; } = 2_000_000;
    public string PersonasConfigPath { get; init; } = "";
    public string PersonaPromptsDirectory { get; init; } = "";
    public string DefaultEngineMode { get; init; } = "hybrid";
    public bool AllowCrossEngineFallback { get; init; } = false;
    public bool PreferLocalForComputer { get; init; } = true;
    public bool PreferHybridForTelegram { get; init; } = true;
    public string SpotifyClientId { get; init; } = "";
    public string SpotifyClientSecret { get; init; } = "";
    public string SpotifyRedirectUri { get; init; } = "http://127.0.0.1:8888/callback";
    public string SpotifyScopes { get; init; } = "user-library-read user-library-modify user-read-playback-state user-modify-playback-state streaming playlist-read-private playlist-read-collaborative playlist-modify-private playlist-modify-public";
    public string OpenWeatherApiKey { get; init; } = "";
    public HashSet<long> AllowedChats { get; init; } = new();
    public long LocalChatId { get; init; } = 0;
    public bool LocalHotkeyEnabled { get; init; } = true;
    public string LocalHotkeyKey { get; init; } = "H";
    public bool LocalVoiceCameraLedEnabled { get; init; } = false;
    public int LocalVoiceCameraIndex { get; init; } = 0;
    public int LocalVoiceSilenceMs { get; init; } = 1200;
    public int LocalVoiceNoVoiceTimeoutMs { get; init; } = 8000;
    public int LocalVoiceMaxSeconds { get; init; } = 20;
    public int LocalVoiceMinSeconds { get; init; } = 1;
    public int LocalVoiceStartRms { get; init; } = 900;
    public int LocalVoiceStopRms { get; init; } = 550;
    public bool MirrorLocalToTelegram { get; init; } = true;
    public bool OverlayEnabled { get; init; } = true;
    public int OverlaySeconds { get; init; } = 12;
    public bool StartupGreetingEnabled { get; init; } = true;
    public bool StartupGreetingTelegramEnabled { get; init; } = true;
    public bool StartupGreetingTelegramOnlyOwner { get; init; } = true;
    public string StartupGreetingText { get; init; } = "Hanna está en línea. Modo local preparado.";
    public string TtsVoice { get; init; } = "es-PE-CamilaNeural";
    public string TtsFallbackVoice { get; init; } = "es-MX-DaliaNeural";
    public string TtsRate { get; init; } = "+4%";
    public string TtsPitch { get; init; } = "-2Hz";
    public string TtsVolume { get; init; } = "+1%";
    public string ProjectsDirectory { get; init; } = "";
    public string AgentOutputDirectory { get; init; } = "";
    public int ProjectIndexMaxFiles { get; init; } = 60;
    public int ProjectIndexMaxChars { get; init; } = 24000;
    public bool AgentOpenGeneratedCode { get; init; } = false;
    public bool ScreenAnalysisEnabled { get; init; } = true;
    public string TokensDirectory { get; init; } = "";
    public string LogsDirectory { get; init; } = "";
    public string ContextDirectory { get; init; } = "";
    public string SettingsDirectory { get; init; } = "";
    public string MemoryDirectory { get; init; } = "";
    public bool MongoEnabled { get; init; } = false;
    public string MongoUri { get; init; } = "";
    public string MongoDatabase { get; init; } = "HannaDB";
    public bool AdminWebEnabled { get; init; } = true;
    public int AdminWebPort { get; init; } = 8787;
    public bool AdminWebOpenBrowserOnStart { get; init; } = false;

    public bool VoiceRecordImmediately { get; init; } = true;
    public int VoiceInitialGraceMs { get; init; } = 900;
    public bool WakeWordEnabled { get; init; } = false;
    public string WakeWords { get; init; } = "Hanna,Oye Hanna,Hey Hanna,Hannah,Hana,Jana,Anna";
    public int WakeWordListenSeconds { get; init; } = 4;

    public bool NodeJsEnabled { get; init; } = true;
    public string NodeExecutable { get; init; } = "node";
    public string NodeProjectsDirectory { get; init; } = "";
    public string VsCodeExecutable { get; init; } = "code";
    public string VisualStudioExecutable { get; init; } = "devenv";

    public bool MySqlEnabled { get; init; } = true;
    public string MySqlConnectionString { get; init; } = "";

    public bool DynamicSkillsEnabled { get; init; } = true;
    public string DynamicSkillsPath { get; init; } = "";
    public bool ContextAlwaysSave { get; init; } = true;
    public string ContextArchiveDirectory { get; init; } = "";

    public bool AssignmentsEnabled { get; init; } = true;
    public string AssignmentsPath { get; init; } = "";
    public int AssignmentPollingMinutes { get; init; } = 10;
    public string AssignmentReminderHours { get; init; } = "24,12,6,3,2,1";
    public string CourseNotebookDirectory { get; init; } = "";

    public bool GoogleIntegrationEnabled { get; init; } = false;
    public string GoogleClientSecretsPath { get; init; } = "";
    public string GoogleTokenDirectory { get; init; } = "";
    public string GoogleClassroomPollMinutes { get; init; } = "15";
    public string ExternalTaskPagesPath { get; init; } = "";

    public bool MobileApiEnabled { get; init; } = true;
    public int MobileApiPort { get; init; } = 8790;
    public string MobileApiBindHost { get; init; } = "127.0.0.1";
    public string MobileApiPairingToken { get; init; } = "";
    public string JwtSecret { get; init; } = "";
    public string JwtIssuer { get; init; } = "Hanna";
    public string JwtAudience { get; init; } = "HannaMobile";
    public int JwtExpireMinutes { get; init; } = 4320;

    // Hanna V6.1: integración real de chat web, fases, memoria jerárquica y multimedia local.
    public bool WebChatEnabled { get; init; } = true;
    public int WebChatPort { get; init; } = 8789;
    public string WebChatDirectory { get; init; } = "";
    public string ActivePhasePath { get; init; } = "";
    public string TieredMemoryRoot { get; init; } = "";
    public string TieredMemoryIndexPath { get; init; } = "";
    public string TieredMemoryDbPath { get; init; } = "";
    public string AuditLogPath { get; init; } = "";
    public bool RbacEnabled { get; init; } = true;
    public string DefaultRole { get; init; } = "usuario";
    public bool NightlyMaintenanceEnabled { get; init; } = false;
    public string NightlyMaintenanceHour { get; init; } = "03:00";
    public string BackupRemote { get; init; } = "";
    public string RcloneExecutable { get; init; } = "rclone";
    public string ZstdExecutable { get; init; } = "zstd";
    public string LgTvBridgeUrl { get; init; } = "";
    public string LgTvNetflixAppId { get; init; } = "netflix";
    public bool NetflixOpenSearchOnly { get; init; } = true;

    public static AppConfig Load()
    {
        string baseDir = DetectBaseDirectory();
        string envPath = Path.Combine(baseDir, "HannaEnv.env");

        if (File.Exists(envPath))
            DotNetEnv.Env.Load(envPath);

        var allowed = new HashSet<long>();
        string? allowedChatsEnv = Environment.GetEnvironmentVariable("TELEGRAM_ALLOWED_CHAT_IDS");

        if (!string.IsNullOrWhiteSpace(allowedChatsEnv))
        {
            foreach (var item in allowedChatsEnv.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (long.TryParse(item.Trim(), out long chatId))
                    allowed.Add(chatId);
            }
        }

        string projectsDirectory = EnvString("HANNA_PROJECTS_DIRECTORY", Path.Combine(baseDir, "proyectos_hanna"));
        string agentOutputDirectory = EnvString("HANNA_AGENT_OUTPUT_DIRECTORY", Path.Combine(baseDir, "salidas_agente"));
        string contextArchiveDirectory = EnvString("HANNA_CONTEXT_ARCHIVE_DIRECTORY", Path.Combine(baseDir, "contexto_persistente"));
        string dynamicSkillsPath = EnvString("HANNA_DYNAMIC_SKILLS_PATH", Path.Combine(baseDir, "configuracion_chats", "dynamic_skills.json"));
        string assignmentsPath = EnvString("HANNA_ASSIGNMENTS_PATH", Path.Combine(baseDir, "configuracion_chats", "assignments.json"));
        string courseNotebookDirectory = EnvString("HANNA_COURSE_NOTEBOOK_DIRECTORY", Path.Combine(baseDir, "cuadernos_hanna"));
        string googleTokenDirectory = EnvString("GOOGLE_TOKEN_DIRECTORY", Path.Combine(baseDir, "google_tokens"));
        string externalTaskPagesPath = EnvString("HANNA_EXTERNAL_TASK_PAGES_PATH", Path.Combine(baseDir, "configuracion_chats", "external_task_pages.json"));
        string nodeProjectsDirectory = EnvString("HANNA_NODE_PROJECTS_DIRECTORY", projectsDirectory);
        string tokenUsageDirectory = ResolvePath(baseDir, EnvString("HANNA_TOKEN_USAGE_DIRECTORY", Path.Combine(baseDir, "token_usage")));
        string personaPromptsDirectory = ResolvePath(baseDir, EnvString("HANNA_PERSONA_PROMPTS_DIRECTORY", Path.Combine(baseDir, "personalidad_modular")));
        string personasConfigPath = ResolvePath(baseDir, EnvString("HANNA_PERSONAS_CONFIG_PATH", Path.Combine(baseDir, "configuracion_chats", "hanna_personas.json")));
        string extrasV6Dir = Path.Combine(baseDir, "extras", "hanna_v6");
        string webChatDir = ResolvePath(baseDir, EnvString("HANNA_WEBCHAT_DIRECTORY", Path.Combine(extrasV6Dir, "HannaWebChat")));
        string activePhasePath = ResolvePath(baseDir, EnvString("HANNA_ACTIVE_PHASE_PATH", Path.Combine(baseDir, "configuracion_chats", "phase_active.txt")));
        string tieredMemoryRoot = ResolvePath(baseDir, EnvString("HANNA_TIERED_MEMORY_ROOT", Path.Combine(baseDir, "memoria_jerarquica")));
        string tieredMemoryIndexPath = ResolvePath(baseDir, EnvString("HANNA_TIERED_MEMORY_INDEX_PATH", Path.Combine(tieredMemoryRoot, "index", "index.jsonl")));
        string tieredMemoryDbPath = ResolvePath(baseDir, EnvString("HANNA_TIERED_MEMORY_DB_PATH", Path.Combine(tieredMemoryRoot, "index", "index.db")));
        string auditLogPath = ResolvePath(baseDir, EnvString("HANNA_AUDIT_LOG_PATH", Path.Combine(baseDir, "auditoria", "audit.jsonl")));

        var config = new AppConfig
        {
            BaseDirectory = baseDir,
            EnvPath = envPath,
            PersonalityPath = Path.Combine(baseDir, "personalidad.txt"),
            TelegramToken = EnvString("TELEGRAM_TOKEN", ""),
            GroqApiKey = EnvString("GROQ_API_KEY", ""),
            GroqChatModel = EnvString("GROQ_CHAT_MODEL", "llama-3.3-70b-versatile"),
            GroqVisionModel = EnvString("GROQ_VISION_MODEL", "meta-llama/llama-4-scout-17b-16e-instruct"),
            GeminiApiKey = EnvString("GEMINI_API_KEY", ""),
            GeminiModel = EnvString("GEMINI_MODEL", "gemini-2.5-flash"),
            GeminiWebVerify = EnvBool("GEMINI_WEB_VERIFY", true),
            OllamaBaseUrl = EnvString("OLLAMA_BASE_URL", "http://localhost:11434").TrimEnd('/'),
            OllamaModel = EnvString("OLLAMA_MODEL", "qwen2.5-coder:3b"),
            OllamaContextSize = EnvInt("OLLAMA_CONTEXT_SIZE", 4096),
            OllamaAutoStart = EnvBool("OLLAMA_AUTO_START", true),
            OllamaExecutable = EnvString("OLLAMA_EXECUTABLE", "ollama"),
            OllamaStartupTimeoutSeconds = EnvInt("OLLAMA_STARTUP_TIMEOUT_SECONDS", 25),
            OpenRouterApiKey = EnvString("OPENROUTER_API_KEY", ""),
            OpenRouterBaseUrl = EnvString("OPENROUTER_BASE_URL", "https://openrouter.ai/api/v1/chat/completions"),
            OpenRouterDefaultModel = EnvString("OPENROUTER_MODEL_DEFAULT", "openai/gpt-4o-mini"),
            OpenRouterArchitectModel = EnvString("OPENROUTER_MODEL_ARCHITECT", "anthropic/claude-3.5-opus"),
            OpenRouterEngineerModel = EnvString("OPENROUTER_MODEL_ENGINEER", "anthropic/claude-3.5-sonnet"),
            OpenRouterOperatorModel = EnvString("OPENROUTER_MODEL_OPERATOR", "google/gemini-2.0-flash"),
            OpenRouterAnalystModel = EnvString("OPENROUTER_MODEL_ANALYST", "meta-llama/llama-3.1-8b-instruct"),
            OpenRouterCreativeModel = EnvString("OPENROUTER_MODEL_CREATIVE", "openai/gpt-4o-mini"),
            OpenRouterAppTitle = EnvString("OPENROUTER_APP_TITLE", "Hanna Astro Soluciones"),
            OpenRouterReferer = EnvString("OPENROUTER_REFERER", "http://localhost"),
            OpenRouterDailyBudgetUsd = EnvDecimal("OPENROUTER_DAILY_BUDGET_USD", 1.00m),
            OpenRouterEstimatedUsdPer1KTokens = EnvDecimal("OPENROUTER_ESTIMATED_USD_PER_1K_TOKENS", 0.001m),
            OpenRouterArchitectUsdPer1KTokens = EnvDecimal("OPENROUTER_ARCHITECT_USD_PER_1K_TOKENS", 0.015m),
            OpenRouterEngineerUsdPer1KTokens = EnvDecimal("OPENROUTER_ENGINEER_USD_PER_1K_TOKENS", 0.003m),
            OpenRouterOperatorUsdPer1KTokens = EnvDecimal("OPENROUTER_OPERATOR_USD_PER_1K_TOKENS", 0.0005m),
            OpenRouterAnalystUsdPer1KTokens = EnvDecimal("OPENROUTER_ANALYST_USD_PER_1K_TOKENS", 0.0000m),
            TokenUsageDirectory = tokenUsageDirectory,
            TokenUploadWarningThreshold = EnvInt("HANNA_TOKEN_UPLOAD_WARNING_THRESHOLD", 8000),
            MaxFileTokenPreviewBytes = EnvLong("HANNA_MAX_FILE_TOKEN_PREVIEW_BYTES", 2000000),
            PersonaPromptsDirectory = personaPromptsDirectory,
            PersonasConfigPath = personasConfigPath,
            DefaultEngineMode = EnvString("HANNA_DEFAULT_ENGINE_MODE", "hybrid"),
            AllowCrossEngineFallback = EnvBool("HANNA_ENGINE_ALLOW_CROSS_FALLBACK", false),
            PreferLocalForComputer = EnvBool("HANNA_PREFER_LOCAL_FOR_COMPUTER", true),
            PreferHybridForTelegram = EnvBool("HANNA_PREFER_HYBRID_FOR_TELEGRAM", true),
            SpotifyClientId = EnvString("SPOTIFY_CLIENT_ID", ""),
            SpotifyClientSecret = EnvString("SPOTIFY_CLIENT_SECRET", ""),
            SpotifyRedirectUri = EnvString("SPOTIFY_REDIRECT_URI", "http://127.0.0.1:8888/callback"),
            SpotifyScopes = EnvString("SPOTIFY_SCOPES", "user-library-read user-library-modify user-read-playback-state user-modify-playback-state streaming playlist-read-private playlist-read-collaborative playlist-modify-private playlist-modify-public"),
            OpenWeatherApiKey = EnvString("OPENWEATHER_API_KEY", ""),
            AllowedChats = allowed,
            LocalChatId = EnvLong("HANNA_LOCAL_CHAT_ID", allowed.FirstOrDefault()),
            LocalHotkeyEnabled = EnvBool("HANNA_LOCAL_HOTKEY_ENABLED", true),
            LocalHotkeyKey = EnvString("HANNA_LOCAL_HOTKEY_KEY", "H"),
            LocalVoiceCameraLedEnabled = EnvBool("HANNA_LOCAL_VOICE_CAMERA_LED_ENABLED", false),
            LocalVoiceCameraIndex = EnvInt("HANNA_LOCAL_VOICE_CAMERA_INDEX", 0),
            LocalVoiceSilenceMs = EnvInt("HANNA_LOCAL_VOICE_SILENCE_MS", 1200),
            LocalVoiceNoVoiceTimeoutMs = EnvInt("HANNA_LOCAL_VOICE_NO_VOICE_TIMEOUT_MS", 8000),
            LocalVoiceMaxSeconds = EnvInt("HANNA_LOCAL_VOICE_MAX_SECONDS", 20),
            LocalVoiceMinSeconds = EnvInt("HANNA_LOCAL_VOICE_MIN_SECONDS", 1),
            LocalVoiceStartRms = EnvInt("HANNA_LOCAL_VOICE_START_RMS", 900),
            LocalVoiceStopRms = EnvInt("HANNA_LOCAL_VOICE_STOP_RMS", 550),
            MirrorLocalToTelegram = EnvBool("HANNA_MIRROR_LOCAL_TO_TELEGRAM", true),
            OverlayEnabled = EnvBool("HANNA_OVERLAY_ENABLED", true),
            OverlaySeconds = EnvInt("HANNA_OVERLAY_SECONDS", 12),
            StartupGreetingEnabled = EnvBool("HANNA_STARTUP_GREETING_ENABLED", true),
            StartupGreetingTelegramEnabled = EnvBool("HANNA_STARTUP_GREETING_TELEGRAM_ENABLED", true),
            StartupGreetingTelegramOnlyOwner = EnvBool("HANNA_STARTUP_GREETING_TELEGRAM_ONLY_OWNER", true),
            StartupGreetingText = EnvString("HANNA_STARTUP_GREETING_TEXT", "Hanna está en línea. Modo local preparado."),
            TtsVoice = EnvString("TTS_VOICE", "es-PE-CamilaNeural"),
            TtsFallbackVoice = EnvString("TTS_FALLBACK_VOICE", "es-MX-DaliaNeural"),
            TtsRate = EnvString("TTS_RATE", "+4%"),
            TtsPitch = EnvString("TTS_PITCH", "-2Hz"),
            TtsVolume = EnvString("TTS_VOLUME", "+1%"),
            ProjectsDirectory = projectsDirectory,
            AgentOutputDirectory = agentOutputDirectory,
            ProjectIndexMaxFiles = EnvInt("HANNA_PROJECT_INDEX_MAX_FILES", 60),
            ProjectIndexMaxChars = EnvInt("HANNA_PROJECT_INDEX_MAX_CHARS", 24000),
            AgentOpenGeneratedCode = EnvBool("HANNA_AGENT_OPEN_GENERATED_CODE", false),
            ScreenAnalysisEnabled = EnvBool("HANNA_SCREEN_ANALYSIS_ENABLED", true),
            TokensDirectory = Path.Combine(baseDir, "spotify_tokens"),
            LogsDirectory = Path.Combine(baseDir, "registros_conversacion"),
            ContextDirectory = Path.Combine(baseDir, "contexto_chats"),
            SettingsDirectory = Path.Combine(baseDir, "configuracion_chats"),
            MemoryDirectory = Path.Combine(baseDir, "memoria_modelos"),
            MongoEnabled = EnvBool("MONGO_ENABLED", false),
            MongoUri = EnvString("MONGO_URI", "mongodb://localhost:27017"),
            MongoDatabase = EnvString("MONGO_DATABASE", "HannaDB"),
            AdminWebEnabled = EnvBool("HANNA_ADMIN_WEB_ENABLED", true),
            AdminWebPort = EnvInt("HANNA_ADMIN_WEB_PORT", 8787),
            AdminWebOpenBrowserOnStart = EnvBool("HANNA_ADMIN_WEB_OPEN_BROWSER", false),
            VoiceRecordImmediately = EnvBool("HANNA_VOICE_RECORD_IMMEDIATELY", true),
            VoiceInitialGraceMs = EnvInt("HANNA_VOICE_INITIAL_GRACE_MS", 900),
            WakeWordEnabled = EnvBool("HANNA_WAKE_WORD_ENABLED", false),
            WakeWords = EnvString("HANNA_WAKE_WORDS", "Hanna,Oye Hanna,Hey Hanna,Hannah,Hana,Jana,Anna"),
            WakeWordListenSeconds = EnvInt("HANNA_WAKE_WORD_LISTEN_SECONDS", 4),
            NodeJsEnabled = EnvBool("HANNA_NODEJS_ENABLED", true),
            NodeExecutable = EnvString("HANNA_NODE_EXECUTABLE", "node"),
            NodeProjectsDirectory = nodeProjectsDirectory,
            VsCodeExecutable = EnvString("HANNA_VSCODE_EXECUTABLE", "code"),
            VisualStudioExecutable = EnvString("HANNA_VISUAL_STUDIO_EXECUTABLE", "devenv"),
            MySqlEnabled = EnvBool("MYSQL_ENABLED", true),
            MySqlConnectionString = EnvString("MYSQL_CONNECTION_STRING", BuildMySqlConnectionString()),
            DynamicSkillsEnabled = EnvBool("HANNA_DYNAMIC_SKILLS_ENABLED", true),
            DynamicSkillsPath = dynamicSkillsPath,
            ContextAlwaysSave = EnvBool("HANNA_CONTEXT_ALWAYS_SAVE", true),
            ContextArchiveDirectory = contextArchiveDirectory,
            AssignmentsEnabled = EnvBool("HANNA_ASSIGNMENTS_ENABLED", true),
            AssignmentsPath = assignmentsPath,
            AssignmentPollingMinutes = EnvInt("HANNA_ASSIGNMENT_POLLING_MINUTES", 10),
            AssignmentReminderHours = EnvString("HANNA_ASSIGNMENT_REMINDER_HOURS", "24,12,6,3,2,1"),
            CourseNotebookDirectory = courseNotebookDirectory,
            GoogleIntegrationEnabled = EnvBool("GOOGLE_INTEGRATION_ENABLED", false),
            GoogleClientSecretsPath = EnvString("GOOGLE_CLIENT_SECRETS_PATH", Path.Combine(baseDir, "google_client_secret.json")),
            GoogleTokenDirectory = googleTokenDirectory,
            GoogleClassroomPollMinutes = EnvString("GOOGLE_CLASSROOM_POLL_MINUTES", "15"),
            ExternalTaskPagesPath = externalTaskPagesPath,
            MobileApiEnabled = EnvBool("HANNA_MOBILE_API_ENABLED", true),
            MobileApiPort = EnvInt("HANNA_MOBILE_API_PORT", 8790),
            MobileApiBindHost = EnvString("HANNA_MOBILE_API_BIND_HOST", "127.0.0.1"),
            MobileApiPairingToken = EnvString("HANNA_MOBILE_API_PAIRING_TOKEN", ""),
            JwtSecret = EnvString("HANNA_JWT_SECRET", ""),
            JwtIssuer = EnvString("HANNA_JWT_ISSUER", "Hanna"),
            JwtAudience = EnvString("HANNA_JWT_AUDIENCE", "HannaMobile"),
            JwtExpireMinutes = EnvInt("HANNA_JWT_EXPIRE_MINUTES", 4320),
            WebChatEnabled = EnvBool("HANNA_WEBCHAT_ENABLED", true),
            WebChatPort = EnvInt("HANNA_WEBCHAT_PORT", 8789),
            WebChatDirectory = webChatDir,
            ActivePhasePath = activePhasePath,
            TieredMemoryRoot = tieredMemoryRoot,
            TieredMemoryIndexPath = tieredMemoryIndexPath,
            TieredMemoryDbPath = tieredMemoryDbPath,
            AuditLogPath = auditLogPath,
            RbacEnabled = EnvBool("HANNA_RBAC_ENABLED", true),
            DefaultRole = EnvString("HANNA_DEFAULT_ROLE", "usuario"),
            NightlyMaintenanceEnabled = EnvBool("HANNA_NIGHTLY_MAINTENANCE_ENABLED", false),
            NightlyMaintenanceHour = EnvString("HANNA_BACKUP_IDLE_HOUR", EnvString("HANNA_NIGHTLY_MAINTENANCE_HOUR", "03:00")),
            BackupRemote = EnvString("HANNA_BACKUP_REMOTE", ""),
            RcloneExecutable = EnvString("HANNA_RCLONE_EXECUTABLE", "rclone"),
            ZstdExecutable = EnvString("HANNA_ZSTD_EXECUTABLE", "zstd"),
            LgTvBridgeUrl = EnvString("HANNA_LGTV_BRIDGE_URL", ""),
            LgTvNetflixAppId = EnvString("HANNA_LGTV_NETFLIX_APP_ID", "netflix"),
            NetflixOpenSearchOnly = EnvBool("HANNA_NETFLIX_OPEN_SEARCH_ONLY", true),
        };

        Directory.CreateDirectory(config.TokensDirectory);
        Directory.CreateDirectory(config.LogsDirectory);
        Directory.CreateDirectory(config.ContextDirectory);
        Directory.CreateDirectory(config.SettingsDirectory);
        Directory.CreateDirectory(config.MemoryDirectory);
        Directory.CreateDirectory(config.TokenUsageDirectory);
        Directory.CreateDirectory(config.PersonaPromptsDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(config.PersonasConfigPath) ?? config.SettingsDirectory);
        Directory.CreateDirectory(config.ProjectsDirectory);
        Directory.CreateDirectory(config.AgentOutputDirectory);
        Directory.CreateDirectory(config.ContextArchiveDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(config.DynamicSkillsPath) ?? config.SettingsDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(config.AssignmentsPath) ?? config.SettingsDirectory);
        Directory.CreateDirectory(config.CourseNotebookDirectory);
        Directory.CreateDirectory(config.GoogleTokenDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(config.ExternalTaskPagesPath) ?? config.SettingsDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(config.ActivePhasePath) ?? config.SettingsDirectory);
        Directory.CreateDirectory(config.TieredMemoryRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(config.TieredMemoryIndexPath) ?? config.TieredMemoryRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(config.TieredMemoryDbPath) ?? config.TieredMemoryRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(config.AuditLogPath) ?? config.BaseDirectory);

        return config;
    }

    private static string BuildMySqlConnectionString()
    {
        string direct = EnvString("MYSQL_CONNECTION_STRING", "");
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;

        string host = EnvString("MYSQL_HOST", "localhost");
        string port = EnvString("MYSQL_PORT", "3306");
        string database = EnvString("MYSQL_DATABASE", "hanna_relacional");
        string user = EnvString("MYSQL_USER", "root");
        string password = EnvString("MYSQL_PASSWORD", "");
        return $"Server={host};Port={port};Database={database};User ID={user};Password={password};SslMode=Preferred;AllowPublicKeyRetrieval=True;";
    }

    private static string DetectBaseDirectory()
    {
        string actual = AppContext.BaseDirectory;

        for (int i = 0; i < 8; i++)
        {
            if (File.Exists(Path.Combine(actual, "HannaEnv.env")) ||
                File.Exists(Path.Combine(actual, "personalidad.txt")) ||
                Directory.GetFiles(actual, "*.csproj").Length > 0)
            {
                return actual;
            }

            DirectoryInfo? parent = Directory.GetParent(actual);

            if (parent == null)
                break;

            actual = parent.FullName;
        }

        return AppContext.BaseDirectory;
    }


    private static string ResolvePath(string baseDir, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return baseDir;

        return Path.IsPathRooted(value) ? value : Path.Combine(baseDir, value);
    }

    private static string EnvString(string key, string fallback)
    {
        string? value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static bool EnvBool(string key, bool fallback)
    {
        string? value = Environment.GetEnvironmentVariable(key);

        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("si", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("sí", StringComparison.OrdinalIgnoreCase);
    }

    private static int EnvInt(string key, int fallback)
    {
        string? value = Environment.GetEnvironmentVariable(key);
        return int.TryParse(value, out int parsed) ? parsed : fallback;
    }

    private static long EnvLong(string key, long fallback)
    {
        string? value = Environment.GetEnvironmentVariable(key);
        return long.TryParse(value, out long parsed) ? parsed : fallback;
    }

    private static decimal EnvDecimal(string key, decimal fallback)
    {
        string? value = Environment.GetEnvironmentVariable(key);
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed) ? parsed : fallback;
    }
}
