using Hanna.Core;
using Hanna.Services;
using Hanna.Skills;
using Hanna.Spotify;
using Telegram.Bot;

namespace Hanna;

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.InputEncoding = Encoding.UTF8;

        var config = AppConfig.Load();
        var runtimeSettings = new RuntimeSettingsService(config);
        using var ollamaDaemon = new OllamaDaemonService(config);

        var httpClient = new HttpClient();
        var mongoLogs = new MongoLogService(config);
        await mongoLogs.Initialize(CancellationToken.None);
        Console.WriteLine("[Arranque] MongoDB listo. Inicializando servicios base V6.4...");

        var storage = new FileStorageService(config);
        var personality = new PersonalityService(config);
        var logs = new ConversationLogService(config);
        var promptPack = new PromptPackService(config);
        var context = new ContextService(config, personality, logs, storage, promptPack);

        var tokenEstimator = new TokenEstimatorService(config);
        var tokenLedger = new TokenUsageLedgerService(config, tokenEstimator);
        var queryAnalyzer = new QueryAnalyzerService();
        var personaService = new HannaPersonaService(config);

        var groq = new GroqService(config, httpClient, context);
        var gemini = new GeminiService(config, httpClient, context);
        var ollama = new OllamaService(config, new HttpClient(), runtimeSettings);
        var openRouter = new OpenRouterService(config, new HttpClient(), queryAnalyzer, personaService, tokenEstimator, tokenLedger);
        var modelMode = new ModelModeService(config, storage);
        var startupProfile = StartupProfile.Resolve(config, runtimeSettings.Snapshot(), modelMode.GetMode());
        startupProfile.PrintPlan();

        if (startupProfile.DecideOllama().ShouldStart)
            await SafeStartAsync("Ollama", () => ollamaDaemon.EnsureRunningAsync(CancellationToken.None));

        var phaseService = new PhaseService(config);
        var tieredMemory = new TieredMemoryService(config);
        var audit = new AuditTrailService(config);
        var nightlyMaintenance = new NightlyMaintenanceService(config, tieredMemory, audit);
        var authorization = new CommandAuthorizationService(config);
        var orchestrator = new ModelOrchestrator(mongoLogs, config, context, groq, gemini, ollama, openRouter, modelMode, tokenLedger, personaService);
        var tts = new TtsService(config, runtimeSettings);
        var response = new ResponseService(config, storage, tts);
        Console.WriteLine("[Arranque] Servicios de IA, fases, memoria, auditorÃ­a y respuesta inicializados.");

        var spotifyAuth = new SpotifyAuthService(config, httpClient, storage);
        var spotifySearch = new SpotifySearchService(config, httpClient);
        var spotifyLibrary = new SpotifyLibraryService(httpClient, spotifyAuth);
        var spotifyPlayback = new SpotifyPlaybackService(config, httpClient, spotifyAuth, storage);

        var youtube = new YoutubeMediaService();
        var weather = new WeatherService(config, httpClient);
        var vision = new VisionService(groq, gemini);
        var memory = new MemoryService(config);
        var reminders = new ReminderService(config);
        var preferences = new PreferencesService(storage);
        var routines = new RoutineService(storage);
        var appLauncher = new AppLauncherService();
        var browser = new BrowserService();
        var mediaControl = new MediaControlService(config, browser);
        var windowsAudio = new WindowsAudioSessionService();
        var trustedWeb = new TrustedWebSearchService(config);
        var fileController = new FileControllerService(config, tokenEstimator, personaService);
        var configUpdate = new ConfigUpdateService(config);
        var communication = new CommunicationService();
        var webVideoDownloader = new WebVideoDownloadService(config);
        var shadowMode = new ShadowModeService(config);
        var hdService = new HornyDownloaderService(config);
        var webcamLed = new WebcamLedService(config.LocalVoiceCameraIndex, runtimeSettings.Snapshot().LocalVoiceCameraLedEnabled);
        var localAudio = new LocalAudioPlaybackService(tts);
        var mirror = new TelegramMirrorService(config, runtimeSettings);
        var contextArchive = new ContextArchiveService(config, runtimeSettings);
        var overlay = new OverlayNotificationService(config, runtimeSettings);
        var codeOutput = new CodeOutputService(config, runtimeSettings);
        var projectContext = new ProjectContextService(config, runtimeSettings);
        var courseNotebooks = new CourseNotebookService(config, runtimeSettings);
        var agentCoding = new AgentCodingService(ollama, context, projectContext, codeOutput);
        var screenCapture = new ScreenCaptureService(config);
        var developerTools = new DeveloperToolService(config, runtimeSettings);
        var googleIntegration = new GoogleIntegrationService(config);
        var dynamicSkills = new DynamicSkillService(config, runtimeSettings, codeOutput, appLauncher, browser, courseNotebooks);
        var assignmentService = new AssignmentService(config, runtimeSettings, mirror, courseNotebooks);
        if (startupProfile.DecideAssignments().ShouldStart)
            SafeStart("Tareas", () => assignmentService.Start());

        if (startupProfile.DecideNightlyMaintenance().ShouldStart)
            SafeStart("Mantenimiento nocturno", () => nightlyMaintenance.Start());

        var intentRouter = new IntentRouter();

        var skills = new List<ISkill>
        {
            new AudioControlSkill(windowsAudio),
            new PersonalityChatSkill(promptPack),
            new TrustedWebSearchSkill(trustedWeb, browser),
            new PersonaSkill(personaService, tokenEstimator, tokenLedger),
            new SystemSkill(config, storage, spotifyAuth, spotifyPlayback, response, webVideoDownloader, shadowMode, hdService),
            new AssistantControlSkill(modelMode, configUpdate, appLauncher, webcamLed),
            new PhaseSkill(phaseService),
            new MemorySkill(memory),
            new PreferencesSkill(preferences),
            new ReminderSkill(reminders),
            new RoutineSkill(routines, preferences, response, spotifyLibrary, spotifyPlayback),
            new MediaControlSkill(mediaControl),
            new DesktopSkill(appLauncher, browser),
            new FileSkill(fileController),
            new CommunicationSkill(communication),
            new WebVideoSkill(config, webVideoDownloader),
            new SpotifySkill(config, spotifyAuth, spotifySearch, spotifyLibrary, spotifyPlayback, response, preferences),
            new YouTubeSkill(spotifySearch, youtube, response),
            new WeatherSkill(weather),
            new VisionSkill(vision, spotifySearch, spotifyLibrary, spotifyPlayback, response),
            new AssignmentSkill(assignmentService, courseNotebooks, googleIntegration),
            new AgentCodingSkill(agentCoding),
            new DynamicSkill(dynamicSkills),
            new GeneralChatSkill(orchestrator, phaseService, tieredMemory)
        };

        var skillRouter = new SkillRouter(intentRouter, skills, audit);
        Console.WriteLine("[Arranque] Skills cargadas: " + skills.Count + ". Preparando voz, hotkeys, API y panel...");
        var telegram = new TelegramService(config, logs, context, response, skillRouter, groq, vision, mongoLogs, modelMode, contextArchive);
        TelegramBotClient? localBotClient = TryCreateTelegramBotClient(config.TelegramToken, startupProfile.DecideTelegram());

        MicrophoneRecorderService? microphoneRecorder = null;
        GlobalHotkeyService? globalHotkey = null;
        WakeWordService? wakeWord = null;

        try
        {
            if (startupProfile.DecideHotkeys().ShouldStart && localBotClient != null)
            {
                microphoneRecorder = new MicrophoneRecorderService();

                var voiceCommand = new VoiceCommandService(
                    config,
                    microphoneRecorder,
                    webcamLed,
                    groq,
                    skillRouter,
                    localBotClient,
                    localAudio,
                    logs,
                    mongoLogs,
                    modelMode,
                    mirror,
                    overlay,
                    codeOutput,
                    runtimeSettings,
                    contextArchive);

                var screenAgent = new ScreenAgentService(
                    config,
                    screenCapture,
                    vision,
                    context,
                    agentCoding,
                    overlay,
                    mirror,
                    logs,
                    mongoLogs,
                    localBotClient,
                    runtimeSettings);

                var hotkeyBindings = new List<HotkeyBinding>
                {
                    new HotkeyBinding(
                        8301,
                        0,
                        GlobalHotkeyService.VkF8,
                        "F8 - voz local sin ventana",
                        () => voiceCommand.ListenOnceAsync(showOverlay: false, CancellationToken.None)),

                    new HotkeyBinding(
                        8302,
                        GlobalHotkeyService.ModControl | GlobalHotkeyService.ModAlt,
                        GlobalHotkeyService.VkEnter,
                        "AltGr+Enter / Ctrl+Alt+Enter - voz local con ventana",
                        () => voiceCommand.ListenOnceAsync(showOverlay: startupProfile.DecideOverlay().ShouldStart, CancellationToken.None)),

                    new HotkeyBinding(
                        8303,
                        GlobalHotkeyService.ModControl | GlobalHotkeyService.ModAlt | GlobalHotkeyService.ModShift,
                        GlobalHotkeyService.VkH,
                        "AltGr+Shift+H / Ctrl+Alt+Shift+H - voz local con ventana",
                        () => voiceCommand.ListenOnceAsync(showOverlay: startupProfile.DecideOverlay().ShouldStart, CancellationToken.None)),
                };

                if (startupProfile.DecideScreenAnalysis().ShouldStart)
                {
                    hotkeyBindings.Add(new HotkeyBinding(
                        8304,
                        0,
                        GlobalHotkeyService.VkF9,
                        "F9 - analizar pantalla y generar cÃ³digo si aplica",
                        () => screenAgent.AnalyzeScreenAsync(CancellationToken.None)));
                }

                globalHotkey = new GlobalHotkeyService(hotkeyBindings);
                globalHotkey.Start();

                if (startupProfile.DecideWakeWord().ShouldStart)
                {
                    wakeWord = new WakeWordService(config, runtimeSettings, groq, voiceCommand);
                    wakeWord.Start();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Hotkey local Error]: {ex.Message}");
        }

        MobileApiServerService? mobileApi = null;
        if (startupProfile.DecideMobileApi().ShouldStart && localBotClient != null)
        {
            mobileApi = new MobileApiServerService(config, runtimeSettings, skillRouter, localBotClient, modelMode, phaseService, tieredMemory, audit, authorization);
            await SafeStartAsync("Mobile API", () => mobileApi.StartAsync(CancellationToken.None));
        }

        AdminWebServerService? adminWeb = null;
        if (startupProfile.DecideAdminWeb().ShouldStart)
        {
            adminWeb = new AdminWebServerService(
                config,
                runtimeSettings,
                modelMode,
                personality,
                localAudio,
                webcamLed,
                ollamaDaemon,
                dynamicSkills,
                assignmentService,
                courseNotebooks,
                developerTools,
                googleIntegration);
            await SafeStartAsync("Admin Web", () => adminWeb.StartAsync(CancellationToken.None));
        }

        WebChatHostService? webChat = null;
        if (startupProfile.DecideWebChat().ShouldStart)
        {
            webChat = new WebChatHostService(config);
            SafeStart("WebChat standalone opcional", () => webChat.Start());
        }

        await logs.RegisterSystem("Hanna iniciÃ³ una nueva sesiÃ³n modular.");

        Console.WriteLine($"Hanna modular estÃ¡ en lÃ­nea. Motor actual: {modelMode.GetModeLabel()}.");
        Console.WriteLine($"Directorio base: {config.BaseDirectory}");
        Console.WriteLine($"Personalidad: {(File.Exists(config.PersonalityPath) ? config.PersonalityPath : "NO ENCONTRADA")}");
        Console.WriteLine($"Registro: {logs.SessionLogPath}");
        RuntimeSettings currentSettings = runtimeSettings.Snapshot();
        Console.WriteLine($"Ollama: {currentSettings.OllamaBaseUrl} | Modelo: {currentSettings.OllamaModel}");
        Console.WriteLine($"OpenRouter: {(string.IsNullOrWhiteSpace(config.OpenRouterApiKey) ? "no configurado" : "configurado")} | Persona: {personaService.GetActivePersona().DisplayName}");
        Console.WriteLine($"Registro tokens: {config.TokenUsageDirectory}");
        Console.WriteLine($"Proyectos agente: {currentSettings.ProjectsDirectory}");
        Console.WriteLine($"Salidas agente: {currentSettings.AgentOutputDirectory}");
        Console.WriteLine($"Panel web: http://127.0.0.1:{currentSettings.AdminWebPort}");
        Console.WriteLine($"Chat online integrado: http://127.0.0.1:{currentSettings.AdminWebPort} (pestaÃ±a Chat online integrado)");
        Console.WriteLine($"Fase activa: {phaseService.GetActivePhase()}");
        Console.WriteLine($"API mÃ³vil: http://{config.MobileApiBindHost}:{config.MobileApiPort} (preparada para Oppo Reno 13 5G)");
        Console.WriteLine($"Cuadernos Hanna: {config.CourseNotebookDirectory}");
        Console.WriteLine("Comandos: /h, /hd, /miid, /modo texto|audio|ambos, /senior, /dev, /ops, /operator, /analyst, /personas, /persona actual, /tokens, /auth, /spotify_status, /dispositivos, /dispositivo 1, /d LINK, /shadow.");
        Console.WriteLine("Motor PC/telÃ©fono: PC usa Ollama local; Telegram usa hÃ­brido si estÃ¡ activado en HannaEnv.env.");
        Console.WriteLine("Voz local: F8 sin ventana, AltGr+Enter con ventana, AltGr+Shift+H con ventana.");
        Console.WriteLine("Pantalla: F9 analiza pantalla y genera cÃ³digo si detecta consigna de programaciÃ³n o SQL.");
        Console.WriteLine("CÃ¡mara: puedes decir 'enciende cÃ¡mara', 'apaga cÃ¡mara', 'activa indicador de cÃ¡mara' o 'desactiva indicador de cÃ¡mara'.");
        Console.WriteLine("Spotify extra: fila, playlists, rutinas y preferencias activadas.");

        if (startupProfile.DecideTelegram().ShouldStart)
            await SafeStartAsync("Telegram", () => telegram.StartAsync());

        RuntimeSettings startupSettings = runtimeSettings.Snapshot();
        if (startupProfile.DecideStartupLocalGreeting().ShouldStart)
        {
            await mirror.MirrorSystem(startupSettings.StartupGreetingText, CancellationToken.None);
            await localAudio.Speak(startupSettings.StartupGreetingText, CancellationToken.None);
        }

        Console.ReadLine();

        webChat?.Dispose();
        adminWeb?.Dispose();
        globalHotkey?.Dispose();
        wakeWord?.Dispose();
        mobileApi?.Dispose();
        nightlyMaintenance.Dispose();
        assignmentService.Dispose();
        microphoneRecorder?.Dispose();
        webcamLed.Dispose();

        await logs.RegisterSystem("Hanna cerrÃ³ desde consola.");
    }


    private static TelegramBotClient? TryCreateTelegramBotClient(string token, StartupDecision decision)
    {
        if (!decision.ShouldStart)
            return null;

        try
        {
            return new TelegramBotClient(token);
        }
        catch (Exception ex) when (ex is ArgumentException or FormatException)
        {
            Console.WriteLine("[Arranque] TelegramBotClient omitido por credenciales: " + ex.Message);
            return null;
        }
    }

    private static void SafeStart(string name, Action action)
    {
        try
        {
            action();
            Console.WriteLine($"[Arranque] {name}: OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Arranque] {name}: ERROR - {ex.Message}");
        }
    }

    private static async Task SafeStartAsync(string name, Func<Task> action)
    {
        try
        {
            await action();
            Console.WriteLine($"[Arranque] {name}: OK");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Arranque] {name}: ERROR - {ex.Message}");
        }
    }
}


