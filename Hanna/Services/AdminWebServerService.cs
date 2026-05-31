using Hanna.Core;
using Hanna.Models;
using System.Net;
using System.Net.Sockets;

namespace Hanna.Services;

internal sealed class AdminWebServerService : IDisposable
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService runtime;
    private readonly ModelModeService modelMode;
    private readonly PersonalityService personality;
    private readonly LocalAudioPlaybackService localAudio;
    private readonly WebcamLedService webcamLed;
    private readonly OllamaDaemonService ollamaDaemon;
    private readonly DynamicSkillService dynamicSkills;
    private readonly AssignmentService assignments;
    private readonly CourseNotebookService notebooks;
    private readonly DeveloperToolService tools;
    private readonly GoogleIntegrationService google;
    private readonly JwtTokenService jwt;
    private readonly WindowsAudioSessionService audioSessions = new();
    private TcpListener? listener;
    private CancellationTokenSource? cts;
    private bool disposed;

    public AdminWebServerService(
        AppConfig config,
        RuntimeSettingsService runtime,
        ModelModeService modelMode,
        PersonalityService personality,
        LocalAudioPlaybackService localAudio,
        WebcamLedService webcamLed,
        OllamaDaemonService ollamaDaemon,
        DynamicSkillService dynamicSkills,
        AssignmentService assignments,
        CourseNotebookService notebooks,
        DeveloperToolService tools,
        GoogleIntegrationService google)
    {
        this.config = config;
        this.runtime = runtime;
        this.modelMode = modelMode;
        this.personality = personality;
        this.localAudio = localAudio;
        this.webcamLed = webcamLed;
        this.ollamaDaemon = ollamaDaemon;
        this.dynamicSkills = dynamicSkills;
        this.assignments = assignments;
        this.notebooks = notebooks;
        this.tools = tools;
        this.google = google;
        jwt = new JwtTokenService(config);
    }

    public string Url => $"http://127.0.0.1:{runtime.Snapshot().AdminWebPort}/";

    public Task StartAsync(CancellationToken cancellationToken)
    {
        RuntimeSettings settings = runtime.Snapshot();
        if (!settings.AdminWebEnabled)
        {
            Console.WriteLine("[Admin Web] Deshabilitado.");
            return Task.CompletedTask;
        }

        try
        {
            listener = new TcpListener(IPAddress.Loopback, Math.Clamp(settings.AdminWebPort, 1024, 65535));
            listener.Start();
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = Task.Run(() => AcceptLoop(cts.Token), CancellationToken.None);
            Console.WriteLine("[Admin Web] Activo en " + Url);
            if (settings.AdminWebOpenBrowserOnStart)
                OpenUrl(Url);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Admin Web Error]: " + ex.Message);
        }
        return Task.CompletedTask;
    }

    private async Task AcceptLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && listener != null)
        {
            TcpClient? client = null;
            try
            {
                client = await listener.AcceptTcpClientAsync(token);
                _ = Task.Run(() => HandleClient(client, token), CancellationToken.None);
            }
            catch { client?.Dispose(); }
        }
    }

    private async Task HandleClient(TcpClient client, CancellationToken token)
    {
        await using NetworkStream stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        try
        {
            string? requestLine = await reader.ReadLineAsync(token);
            if (string.IsNullOrWhiteSpace(requestLine)) return;
            string[] parts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            string method = parts.Length > 0 ? parts[0].ToUpperInvariant() : "GET";
            string rawPath = parts.Length > 1 ? parts[1] : "/";
            string path = rawPath.Split('?', 2)[0];
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                string? line = await reader.ReadLineAsync(token);
                if (string.IsNullOrEmpty(line)) break;
                int idx = line.IndexOf(':');
                if (idx > 0) headers[line[..idx].Trim()] = line[(idx + 1)..].Trim();
            }
            string body = "";
            if (headers.TryGetValue("Content-Length", out string? lengthText) && int.TryParse(lengthText, out int length) && length > 0)
            {
                char[] buffer = new char[length];
                int read = 0;
                while (read < length)
                {
                    int current = await reader.ReadAsync(buffer.AsMemory(read, length - read), token);
                    if (current <= 0) break;
                    read += current;
                }
                body = new string(buffer, 0, read);
            }
            await Route(method, path, rawPath, body, stream, token);
        }
        catch (Exception ex)
        {
            await WriteJson(stream, 500, new { ok = false, error = ex.Message }, token);
        }
        finally { client.Dispose(); }
    }

    private async Task Route(string method, string path, string rawPath, string body, Stream stream, CancellationToken token)
    {
        if (method == "OPTIONS") { await WriteJson(stream, 200, new { ok = true }, token); return; }
        if (method == "GET" && (path == "/" || path == "/index.html")) { await WriteHtml(stream, Html, token); return; }
        if (method == "GET" && path == "/api/state") { await WriteJson(stream, 200, await BuildState(token), token); return; }
        if (method == "GET" && path == "/api/tools") { await WriteJson(stream, 200, await tools.BuildStatus(token), token); return; }
        if (method == "GET" && path == "/api/google") { await WriteJson(stream, 200, google.BuildStatus(), token); return; }
        if (method == "GET" && path == "/api/audio/sessions") { await WriteJson(stream, 200, new { ok = true, sessions = audioSessions.ListSessions() }, token); return; }
        if (method == "GET" && path == "/api/access/status") { await WriteJson(stream, 200, BuildAccessStatus(), token); return; }
        if (method == "GET" && path == "/api/panel-chat/config") { await WriteJson(stream, 200, BuildPanelChatConfig(), token); return; }
        if (method == "GET" && path == "/api/skills") { await WriteJson(stream, 200, dynamicSkills.List(), token); return; }
        if (method == "GET" && path == "/api/phases") { await WriteJson(stream, 200, BuildPhases(), token); return; }
        if (method == "GET" && path == "/api/assignments") { await WriteJson(stream, 200, assignments.List(), token); return; }
        if (method == "GET" && path == "/api/notebooks") { await WriteJson(stream, 200, notebooks.ListNotebooks(), token); return; }
        if (method == "GET" && path == "/api/files") { await WriteJson(stream, 200, ListFiles(GetQuery(rawPath, "root"), GetQuery(rawPath, "dir")), token); return; }
        if (method == "GET" && path == "/api/file") { await WriteJson(stream, 200, ReadFile(GetQuery(rawPath, "path")), token); return; }

        if (method == "POST" && path == "/api/access/token")
        {
            string pairingToken = ReadString(body, "pairingToken", "token");
            if (!IsPairingTokenValid(pairingToken)) { await WriteJson(stream, 401, new { ok = false, error = "Pairing token inválido" }, token); return; }
            string chatText = ReadString(body, "telegramChatId", "telegram_chat_id", "chatId");
            if (!long.TryParse(chatText, out long chatId) || !IsChatAllowed(chatId)) { await WriteJson(stream, 403, new { ok = false, error = "ChatId no permitido" }, token); return; }
            if (!jwt.IsConfigured) { await WriteJson(stream, 500, new { ok = false, error = "Configura HANNA_JWT_SECRET en HannaEnv.env" }, token); return; }
            string accessToken = jwt.CreateMobileToken(chatId, chatId == config.LocalChatId ? "dueno" : "usuario", "Hanna Admin");
            await WriteJson(stream, 200, new { ok = true, access_token = accessToken, token_type = "bearer", authorization = "Bearer " + accessToken }, token); return;
        }

        if (method == "POST" && path == "/api/settings")
        {
            RuntimeSettings? incoming = JsonSerializer.Deserialize<RuntimeSettings>(body, JsonOptions());
            if (incoming == null) { await WriteJson(stream, 400, new { ok = false, error = "JSON inválido" }, token); return; }
            RuntimeSettings saved = runtime.Replace(incoming);
            webcamLed.SetAutoIndicator(saved.LocalVoiceCameraLedEnabled);
            await WriteJson(stream, 200, new { ok = true, settings = saved, note = "Algunas funciones requieren reinicio para aplicarse completamente." }, token); return;
        }
        if (method == "POST" && path == "/api/settings/toggle")
        {
            string key = ReadString(body, "key", "name");
            bool enabled = ReadBool(body, "enabled", "value");
            string result = ApplyToggle(key, enabled);
            await WriteJson(stream, 200, new { ok = true, key, enabled, result, settings = runtime.Snapshot() }, token); return;
        }
        if (method == "POST" && path == "/api/engine")
        {
            EngineMode parsed = ParseEngine(ReadString(body, "mode", "engine", "motor"));
            await modelMode.SetMode(parsed, token);
            await WriteJson(stream, 200, new { ok = true, engine = modelMode.GetModeLabel() }, token); return;
        }
        if (method == "POST" && path == "/api/phase")
        {
            string phase = NormalizePhase(ReadString(body, "phase", "fase"));
            Directory.CreateDirectory(Path.GetDirectoryName(config.ActivePhasePath) ?? config.SettingsDirectory);
            await File.WriteAllTextAsync(config.ActivePhasePath, phase, Encoding.UTF8, token);
            await WriteJson(stream, 200, new { ok = true, phase }, token); return;
        }
        if (method == "POST" && path == "/api/personality")
        {
            await File.WriteAllTextAsync(config.PersonalityPath, ReadString(body, "text"), Encoding.UTF8, token);
            await WriteJson(stream, 200, new { ok = true }, token); return;
        }
        if (method == "POST" && path == "/api/audio/app-volume")
        {
            string app = ReadString(body, "app", "application");
            string percentText = ReadString(body, "percent", "volume");
            if (!float.TryParse(percentText, out float percent)) percent = 30;
            string result = string.IsNullOrWhiteSpace(app) ? audioSessions.SetMasterVolume(percent) : audioSessions.SetApplicationVolume(app, percent);
            await WriteJson(stream, 200, new { ok = true, result }, token); return;
        }
        if (method == "POST" && path == "/api/test-tts")
        {
            string text = ReadString(body, "text");
            await localAudio.Speak(string.IsNullOrWhiteSpace(text) ? "Hola, soy Hanna. Prueba de voz desde el panel." : text, token);
            await WriteJson(stream, 200, new { ok = true }, token); return;
        }
        if (method == "POST" && path == "/api/camera")
        {
            string action = ReadString(body, "action").ToLowerInvariant();
            string result = action switch
            {
                "on" or "encender" => webcamLed.TurnOn(),
                "off" or "apagar" => webcamLed.TurnOff(),
                "auto_on" => webcamLed.SetAutoIndicator(true),
                "auto_off" => webcamLed.SetAutoIndicator(false),
                _ => webcamLed.Toggle()
            };
            runtime.Update(s => { s.LocalVoiceCameraLedEnabled = webcamLed.AutoIndicatorEnabled; return s; });
            await WriteJson(stream, 200, new { ok = true, message = result, cameraOpen = webcamLed.IsCameraOpen, auto = webcamLed.AutoIndicatorEnabled }, token); return;
        }
        if (method == "POST" && path == "/api/open-folder")
        {
            string folder = ResolveFolder(ReadString(body, "kind"));
            Directory.CreateDirectory(folder);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
            await WriteJson(stream, 200, new { ok = true, folder }, token); return;
        }
        if (method == "POST" && path == "/api/skills")
        {
            var skill = JsonSerializer.Deserialize<DynamicSkillDefinition>(body, JsonOptions());
            if (skill == null) { await WriteJson(stream, 400, new { ok = false, error = "Skill inválida" }, token); return; }
            await WriteJson(stream, 200, new { ok = true, skill = dynamicSkills.Upsert(skill) }, token); return;
        }
        if (method == "POST" && path == "/api/skills/all")
        {
            var skills = JsonSerializer.Deserialize<List<DynamicSkillDefinition>>(body, JsonOptions()) ?? new List<DynamicSkillDefinition>();
            dynamicSkills.SaveAll(skills);
            await WriteJson(stream, 200, new { ok = true }, token); return;
        }
        if (method == "POST" && path == "/api/assignments")
        {
            var item = JsonSerializer.Deserialize<AssignmentItem>(body, JsonOptions());
            if (item == null) { await WriteJson(stream, 400, new { ok = false, error = "Tarea inválida" }, token); return; }
            await WriteJson(stream, 200, new { ok = true, assignment = assignments.Upsert(item) }, token); return;
        }
        if (method == "POST" && path == "/api/assignments/check")
        {
            await assignments.CheckAndNotify(token);
            await WriteJson(stream, 200, new { ok = true }, token); return;
        }
        if (method == "POST" && path == "/api/notebooks/create")
        {
            string folder = notebooks.CreateOrUpdateNotebook(ReadString(body, "subject"), "Panel web", ReadString(body, "content"));
            await WriteJson(stream, 200, new { ok = true, folder }, token); return;
        }
        if (method == "POST" && path == "/api/file")
        {
            string filePath = ReadString(body, "path");
            string content = ReadString(body, "content");
            string? safe = SafePath(filePath);
            if (safe == null) { await WriteJson(stream, 400, new { ok = false, error = "Ruta no permitida" }, token); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(safe) ?? config.BaseDirectory);
            await File.WriteAllTextAsync(safe, content ?? "", Encoding.UTF8, token);
            await WriteJson(stream, 200, new { ok = true, path = safe }, token); return;
        }
        if (method == "POST" && path == "/api/open-browser") { OpenUrl(Url); await WriteJson(stream, 200, new { ok = true }, token); return; }
        if (method == "POST" && path == "/api/restart") { await WriteJson(stream, 200, new { ok = true, message = "Hanna se reiniciará en unos segundos." }, token); ScheduleRestart(); return; }
        if (method == "POST" && path == "/api/shutdown") { await WriteJson(stream, 200, new { ok = true, message = "Hanna se cerrará." }, token); ScheduleShutdown(); return; }

        await WriteJson(stream, 404, new { ok = false, error = "Ruta no encontrada" }, token);
    }

    private async Task<object> BuildState(CancellationToken token)
    {
        RuntimeSettings snapshot = runtime.Snapshot();
        string personalityText = File.Exists(config.PersonalityPath) ? await File.ReadAllTextAsync(config.PersonalityPath, Encoding.UTF8, token) : "";
        bool ollamaAvailable = await ollamaDaemon.IsAvailable(token);
        return new
        {
            ok = true,
            engine = modelMode.GetModeLabel(),
            settings = snapshot,
            personality = personalityText,
            access = BuildAccessStatus(),
            status = new
            {
                config.BaseDirectory,
                config.EnvPath,
                runtimePath = Path.Combine(config.SettingsDirectory, "runtime_settings.json"),
                ollamaAvailable,
                cameraOpen = webcamLed.IsCameraOpen,
                cameraAuto = webcamLed.AutoIndicatorEnabled,
                projectsExists = Directory.Exists(snapshot.ProjectsDirectory),
                outputsExists = Directory.Exists(snapshot.AgentOutputDirectory),
                config.MongoEnabled,
                config.MongoDatabase,
                config.LocalChatId,
                dynamicSkillsPath = config.DynamicSkillsPath,
                assignmentsPath = config.AssignmentsPath,
                notebooks = config.CourseNotebookDirectory,
                mobileApi = $"http://{config.MobileApiBindHost}:{config.MobileApiPort}",
                panelChat = "Integrado en el panel web 8787",
                standaloneWebChat = $"http://127.0.0.1:{config.WebChatPort}",
                webChatStandaloneEnabled = config.WebChatEnabled,
                activePhasePath = config.ActivePhasePath,
                activePhase = File.Exists(config.ActivePhasePath) ? File.ReadAllText(config.ActivePhasePath, Encoding.UTF8).Trim() : "local",
                tieredMemoryEnabled = !string.IsNullOrWhiteSpace(config.TieredMemoryDbPath),
                tieredMemoryRoot = config.TieredMemoryRoot,
                tieredMemoryDbPath = config.TieredMemoryDbPath,
                tieredMemoryDbExists = File.Exists(config.TieredMemoryDbPath),
                auditLogEnabled = !string.IsNullOrWhiteSpace(config.AuditLogPath),
                auditLogPath = config.AuditLogPath,
                auditLogExists = File.Exists(config.AuditLogPath),
                rbacEnabled = config.RbacEnabled,
                nightlyMaintenanceEnabled = config.NightlyMaintenanceEnabled,
                backupRemote = config.BackupRemote
            },
            commands = CommandCatalog()
        };
    }

    private object BuildPanelChatConfig()
    {
        return new
        {
            ok = true,
            mobileApiBase = $"http://{config.MobileApiBindHost}:{config.MobileApiPort}",
            localChatId = config.LocalChatId,
            currentEngine = modelMode.GetModeLabel(),
            engines = new[]
            {
                new { id = "ollama", label = "Ollama local" },
                new { id = "gemini", label = "Gemini directo" },
                new { id = "groq", label = "Groq directo" },
                new { id = "openrouter", label = "OpenRouter" },
                new { id = "hybrid", label = "Híbrido" },
                new { id = "original", label = "Original" }
            },
            phases = new[]
            {
                new { id = "local", label = "Local / Offline" },
                new { id = "ahorro", label = "Ahorro" },
                new { id = "programacion", label = "Programación" },
                new { id = "multimedia", label = "Multimedia" },
                new { id = "ops", label = "Operaciones" },
                new { id = "estudio", label = "Estudio" },
                new { id = "nube", label = "Nube" },
                new { id = "architect", label = "Arquitectura" }
            }
        };
    }

    private object BuildAccessStatus()
    {
        return new
        {
            ok = true,
            adminWeb = Url,
            mobileApi = $"http://{config.MobileApiBindHost}:{config.MobileApiPort}",
            jwtConfigured = jwt.IsConfigured,
            pairingConfigured = !string.IsNullOrWhiteSpace(config.MobileApiPairingToken),
            localChatId = config.LocalChatId,
            allowedChats = config.AllowedChats,
            jwt = new
            {
                issuer = config.JwtIssuer,
                audience = config.JwtAudience,
                expireMinutes = config.JwtExpireMinutes,
                endpoints = new[]
                {
                    "POST /api/access/token",
                    "POST /api/mobile/auth/telegram-login",
                    "GET /api/mobile/auth/status",
                    "POST /api/mobile/message"
                }
            },
            apiProjects = new[]
            {
                new { name = "Hanna Mobile API", baseUrl = $"http://{config.MobileApiBindHost}:{config.MobileApiPort}", auth = "JWT o pairing token" },
                new { name = "Hanna Admin API", baseUrl = Url.TrimEnd('/'), auth = "local only" },
                new { name = "FastAPI MySQL+Mongo JWT", baseUrl = "http://127.0.0.1:8000", auth = "JWT por /auth/telegram-login" },
                new { name = "C# API MySQL+Mongo JWT", baseUrl = "http://localhost:5000", auth = "JWT por /auth/telegram-login" }
            }
        };
    }

    private string ApplyToggle(string key, bool enabled)
    {
        key = (key ?? "").Trim();
        string note = "Cambio aplicado.";
        RuntimeSettings saved = runtime.Update(s =>
        {
            switch (key)
            {
                case "adminWebEnabled": s.AdminWebEnabled = enabled; note = "Requiere reinicio para apagar/encender el servidor web."; break;
                case "mobileApiEnabled": s.MobileApiEnabled = enabled; note = "Requiere reinicio para apagar/encender la API móvil."; break;
                case "wakeWordEnabled": s.WakeWordEnabled = enabled; note = "Puede requerir reinicio para iniciar/detener escucha permanente."; break;
                case "dynamicSkillsEnabled": s.DynamicSkillsEnabled = enabled; break;
                case "assignmentsEnabled": s.AssignmentsEnabled = enabled; break;
                case "googleIntegrationEnabled": s.GoogleIntegrationEnabled = enabled; break;
                case "screenAnalysisEnabled": s.ScreenAnalysisEnabled = enabled; break;
                case "mirrorLocalToTelegram": s.MirrorLocalToTelegram = enabled; break;
                case "overlayEnabled": s.OverlayEnabled = enabled; break;
                case "localHotkeyEnabled": s.LocalHotkeyEnabled = enabled; note = "Requiere reinicio para registrar/desregistrar hotkeys."; break;
                case "agentOpenGeneratedCode": s.AgentOpenGeneratedCode = enabled; break;
                case "localVoiceCameraLedEnabled": s.LocalVoiceCameraLedEnabled = enabled; webcamLed.SetAutoIndicator(enabled); break;
                case "voiceRecordImmediately": s.VoiceRecordImmediately = enabled; break;
                case "ollamaAutoStart": s.OllamaAutoStart = enabled; note = "Se aplicará por completo al siguiente arranque."; break;
                case "startupGreetingEnabled": s.StartupGreetingEnabled = enabled; break;
                case "preferLocalForComputer": s.PreferLocalForComputer = enabled; break;
                case "preferHybridForTelegram": s.PreferHybridForTelegram = enabled; break;
                default: note = "Switch no reconocido."; break;
            }
            return s;
        });
        return note;
    }

    private string ResolveFolder(string kind)
    {
        RuntimeSettings s = runtime.Snapshot();
        return kind switch
        {
            "base" => config.BaseDirectory,
            "logs" => config.LogsDirectory,
            "settings" => config.SettingsDirectory,
            "projects" => s.ProjectsDirectory,
            "outputs" => s.AgentOutputDirectory,
            "notebooks" => config.CourseNotebookDirectory,
            "context" => config.ContextArchiveDirectory,
            "hanna" => Path.Combine(config.BaseDirectory, "prompts_hanna"),
            "profiles" => Path.Combine(config.BaseDirectory, "chat_profiles"),
            "self" => Path.Combine(config.BaseDirectory, "hanna_self_knowledge"),
            _ => config.BaseDirectory
        };
    }

    private object ListFiles(string rootKind, string dir)
    {
        string root = ResolveFolder(string.IsNullOrWhiteSpace(rootKind) ? "base" : rootKind);
        string requested = string.IsNullOrWhiteSpace(dir) ? root : Path.GetFullPath(Path.Combine(root, dir));
        if (!requested.StartsWith(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
            requested = root;
        Directory.CreateDirectory(requested);
        return new
        {
            root,
            dir = requested,
            folders = Directory.GetDirectories(requested).Select(x => new { name = Path.GetFileName(x), path = x }).OrderBy(x => x.name).Take(200),
            files = Directory.GetFiles(requested).Where(IsWebEditable).Select(x => new { name = Path.GetFileName(x), path = x, size = new FileInfo(x).Length }).OrderBy(x => x.name).Take(300)
        };
    }

    private object ReadFile(string path)
    {
        string? safe = SafePath(path);
        if (safe == null || !File.Exists(safe))
            return new { ok = false, error = "Archivo no permitido o no encontrado" };
        return new { ok = true, path = safe, content = File.ReadAllText(safe, Encoding.UTF8) };
    }

    private string? SafePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        string full = Path.GetFullPath(path);
        string[] roots = { config.BaseDirectory, runtime.Snapshot().ProjectsDirectory, runtime.Snapshot().AgentOutputDirectory, config.CourseNotebookDirectory, config.ContextArchiveDirectory };
        return roots.Any(r => !string.IsNullOrWhiteSpace(r) && full.StartsWith(Path.GetFullPath(r), StringComparison.OrdinalIgnoreCase)) ? full : null;
    }

    private static bool IsWebEditable(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        string[] ok = { ".cs", ".json", ".txt", ".md", ".sql", ".env", ".example", ".js", ".ts", ".html", ".css", ".xml", ".csproj" };
        if (path.Contains("\\bin\\", StringComparison.OrdinalIgnoreCase) || path.Contains("\\obj\\", StringComparison.OrdinalIgnoreCase) || path.Contains("\\.vs\\", StringComparison.OrdinalIgnoreCase)) return false;
        return ok.Contains(ext) || Path.GetFileName(path).Contains("HannaEnv", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsPairingTokenValid(string token)
    {
        return !string.IsNullOrWhiteSpace(config.MobileApiPairingToken) && token == config.MobileApiPairingToken;
    }

    private bool IsChatAllowed(long chatId)
    {
        return chatId == config.LocalChatId || config.AllowedChats.Contains(chatId);
    }

    private void ScheduleRestart()
    {
        _ = Task.Run(async () =>
        {
            await Task.Delay(900);
            try
            {
                string csproj = Path.Combine(config.BaseDirectory, "Hanna.csproj");
                if (File.Exists(csproj))
                {
                    Process.Start(new ProcessStartInfo("cmd.exe", $"/c start \"Hanna\" dotnet run --project \"{csproj}\"") { UseShellExecute = false, CreateNoWindow = true });
                }
                else if (!string.IsNullOrWhiteSpace(Environment.ProcessPath))
                {
                    Process.Start(new ProcessStartInfo(Environment.ProcessPath) { UseShellExecute = true });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Admin Web Restart Error]: " + ex.Message);
            }
            Environment.Exit(0);
        });
    }

    private static void ScheduleShutdown()
    {
        _ = Task.Run(async () => { await Task.Delay(700); Environment.Exit(0); });
    }

    private static object[] CommandCatalog() => new object[]
    {
        new { comando = "/h", descripcion = "Muestra ayuda y comandos disponibles." },
        new { comando = "F8", descripcion = "Voz local sin ventana. Graba inmediatamente y se corta por silencio." },
        new { comando = "AltGr + Enter", descripcion = "Voz local con overlay." },
        new { comando = "F9", descripcion = "Analiza pantalla y genera .txt si detecta consigna de código/SQL." },
        new { comando = "Hanna / Oye Hanna", descripcion = "Wake word opcional. Se activa desde el panel; consume más recursos." },
        new { comando = "reproduce mi playlist ...", descripcion = "Spotify abre/reproduce playlist, álbum o canción en una sola orden." },
        new { comando = "Hanna necesito este código...", descripcion = "Genera código con contexto de tus proyectos y lo guarda en HannaAgentOutputs." },
        new { comando = "crea tarea de materia ...", descripcion = "Registra tarea, cuaderno local y recordatorios 24/12/6/3/2/1 horas." },
        new { comando = "crea cuaderno de materia ...", descripcion = "Crea cuaderno local tipo NotebookLM para estudiar." },
        new { comando = "enciende cámara / apaga cámara", descripcion = "Controla el indicador de cámara." },
        new { comando = "Panel web", descripcion = "Configura motores, voz, proyectos, skills, tareas, archivos, JWT y móvil." }
    };

    private static EngineMode ParseEngine(string value)
    {
        value = (value ?? "").Trim().ToLowerInvariant();
        return value switch
        {
            "groq" => EngineMode.GroqOnly,
            "gemini" => EngineMode.GeminiOnly,
            "hybrid" or "hibrido" or "híbrido" => EngineMode.Hybrid,
            "ollama" or "local" => EngineMode.OllamaLocal,
            "openrouter" or "open router" or "openroutes" or "open routes" or "open_router" => EngineMode.OpenRouter,
            _ => EngineMode.Original
        };
    }

    private object BuildPhases()
    {
        string path = config.ActivePhasePath;
        string current = File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8).Trim() : "local";
        return new
        {
            ok = true,
            current = NormalizePhase(current),
            phases = new[]
            {
                new { id = "local", label = "Local / Offline" },
                new { id = "ahorro", label = "Ahorro" },
                new { id = "programacion", label = "Programación" },
                new { id = "multimedia", label = "Multimedia" },
                new { id = "ops", label = "Operaciones" },
                new { id = "estudio", label = "Estudio" },
                new { id = "nube", label = "Nube" },
                new { id = "architect", label = "Arquitectura" }
            }
        };
    }

    private static string NormalizePhase(string value)
    {
        value = (value ?? "").Trim().ToLowerInvariant().Replace("programación", "programacion").Replace("arquitectura", "architect");
        return value switch
        {
            "local" or "ahorro" or "programacion" or "multimedia" or "ops" or "estudio" or "nube" or "architect" => value,
            _ => "local"
        };
    }

    private static string GetQuery(string rawPath, string key)
    {
        int idx = rawPath.IndexOf('?');
        if (idx < 0) return "";
        foreach (string part in rawPath[(idx + 1)..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            string[] kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0].Equals(key, StringComparison.OrdinalIgnoreCase))
                return WebUtility.UrlDecode(kv[1]);
        }
        return "";
    }

    private static string ReadString(string body, params string[] properties)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            foreach (string property in properties)
            {
                if (doc.RootElement.TryGetProperty(property, out JsonElement el))
                    return el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.ToString();
            }
        }
        catch { }
        return "";
    }

    private static bool ReadBool(string body, params string[] properties)
    {
        try
        {
            using JsonDocument doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
            foreach (string property in properties)
            {
                if (doc.RootElement.TryGetProperty(property, out JsonElement el))
                {
                    if (el.ValueKind == JsonValueKind.True) return true;
                    if (el.ValueKind == JsonValueKind.False) return false;
                    if (bool.TryParse(el.ToString(), out bool result)) return result;
                }
            }
        }
        catch { }
        return false;
    }

    private static async Task WriteHtml(Stream stream, string html, CancellationToken token)
    {
        byte[] body = Encoding.UTF8.GetBytes(html);
        await WriteHeader(stream, 200, "text/html; charset=utf-8", body.Length, token);
        await stream.WriteAsync(body, token);
    }

    private static async Task WriteJson(Stream stream, int statusCode, object value, CancellationToken token)
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions()));
        await WriteHeader(stream, statusCode, "application/json; charset=utf-8", body.Length, token);
        await stream.WriteAsync(body, token);
    }

    private static async Task WriteHeader(Stream stream, int statusCode, string contentType, int length, CancellationToken token)
    {
        string status = statusCode switch { 200 => "OK", 400 => "Bad Request", 401 => "Unauthorized", 403 => "Forbidden", 404 => "Not Found", 500 => "Internal Server Error", _ => "OK" };
        string header = $"HTTP/1.1 {statusCode} {status}\r\nContent-Type: {contentType}\r\nCache-Control: no-store\r\nAccess-Control-Allow-Origin: *\r\nAccess-Control-Allow-Headers: Authorization, Content-Type\r\nAccess-Control-Allow-Methods: GET, POST, OPTIONS\r\nContent-Length: {length}\r\nConnection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(header), token);
    }

    private static JsonSerializerOptions JsonOptions() => new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
    private static void OpenUrl(string url) { try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { } }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try { cts?.Cancel(); } catch { }
        try { listener?.Stop(); } catch { }
        try { cts?.Dispose(); } catch { }
    }

    private const string Html = """
<!doctype html>
<html lang="es">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>Hanna V6.4 Panel Integrado</title>
<style>
:root{--bg:#07111f;--card:#0e1a2b;--card2:#12233a;--line:#27415f;--text:#eef6ff;--muted:#9fb0c6;--accent:#65d6ff;--accent2:#a78bfa;--good:#22c55e;--warn:#f59e0b;--bad:#ef4444;--shadow:0 18px 55px #0008}*{box-sizing:border-box}body{margin:0;color:var(--text);font-family:Segoe UI,Arial,sans-serif;background:radial-gradient(circle at 10% 0,#2563eb55,transparent 30%),radial-gradient(circle at 90% 0,#7c3aed33,transparent 25%),var(--bg)}button,input,select,textarea{font:inherit}.shell{display:grid;grid-template-columns:292px 1fr;gap:16px;min-height:100vh;padding:16px}.side{position:sticky;top:16px;height:calc(100vh - 32px);overflow:auto;background:#081425d9;border:1px solid var(--line);border-radius:24px;padding:16px;box-shadow:var(--shadow)}.brand{display:flex;gap:12px;align-items:center;margin-bottom:16px}.logo{width:44px;height:44px;border-radius:16px;background:linear-gradient(135deg,var(--accent),var(--accent2));display:grid;place-items:center;color:#03111d;font-weight:900}.brand h1{font-size:22px;margin:0}.brand p{margin:2px 0 0;color:var(--muted);font-size:12px}.nav button{width:100%;padding:12px 13px;margin:5px 0;border:1px solid transparent;border-radius:14px;background:#102036;color:#dce9fa;text-align:left;cursor:pointer}.nav button.active,.nav button:hover{background:linear-gradient(135deg,#63d5ff,#a78bfa);color:#061120;font-weight:800}.main{display:flex;flex-direction:column;gap:14px}.top{display:grid;grid-template-columns:1.4fr .8fr .8fr .8fr;gap:12px}.card{background:#0e1a2bdd;border:1px solid var(--line);border-radius:22px;padding:16px;box-shadow:var(--shadow)}.card h2,.card h3{margin:0 0 9px}.muted{color:var(--muted);font-size:13px;line-height:1.45}.grid{display:grid;grid-template-columns:repeat(auto-fit,minmax(295px,1fr));gap:12px}.two{display:grid;grid-template-columns:1fr 1fr;gap:12px}.hidden{display:none!important}.pill{display:inline-block;border:1px solid #365675;border-radius:999px;padding:5px 9px;margin:3px;color:#cfe5ff;background:#0a1728;font-size:12px}.good{color:#86efac}.bad{color:#fca5a5}.warn{color:#fcd34d}.big{font-size:28px;font-weight:900}.metric small{display:block;color:var(--muted)}label{display:block;color:#c9d8ed;font-size:13px;margin-top:8px}input,select,textarea{width:100%;border:1px solid #34516f;background:#07111f;color:var(--text);border-radius:14px;padding:11px;margin-top:5px}textarea{min-height:115px;resize:vertical}.primary,.secondary,.danger,.goodBtn{border:0;border-radius:14px;padding:11px 14px;margin:5px 5px 5px 0;cursor:pointer;font-weight:800}.primary{background:linear-gradient(135deg,var(--accent),var(--accent2));color:#03111d}.secondary{background:#17283e;color:#e9f2ff;border:1px solid #35536e}.danger{background:var(--bad);color:white}.goodBtn{background:var(--good);color:#031409}pre{white-space:pre-wrap;background:#07111f;border:1px solid #203956;border-radius:14px;padding:12px;max-height:340px;overflow:auto;color:#d9e8fb}.chatBox{height:455px;overflow:auto;background:#07111f;border:1px solid #203956;border-radius:18px;padding:12px}.bubble{max-width:86%;padding:11px 13px;border-radius:16px;margin:8px 0;white-space:pre-wrap}.bubble.user{margin-left:auto;background:#1d4ed8}.bubble.bot{background:#17283e}.bubble.sys{background:#3b2b13;color:#ffe8bd}.composer{display:grid;grid-template-columns:1fr 120px;gap:8px;margin-top:10px}.switch{display:flex;align-items:center;justify-content:space-between;gap:12px;background:#07111f;border:1px solid #203956;border-radius:16px;padding:12px;margin:8px 0}.toggle{appearance:none;width:54px;height:30px;background:#334155;border-radius:999px;position:relative;outline:none}.toggle:checked{background:var(--good)}.toggle:before{content:"";position:absolute;width:24px;height:24px;left:3px;top:3px;border-radius:50%;background:white;transition:.2s}.toggle:checked:before{left:27px}.files a{display:block;color:#9bdcff;text-decoration:none;margin:5px 0}.toast{position:fixed;right:18px;bottom:18px;background:#061120;border:1px solid #365675;border-radius:16px;padding:13px;display:none;box-shadow:var(--shadow);z-index:9}@media(max-width:900px){.shell{grid-template-columns:1fr}.side{position:relative;height:auto}.top,.two{grid-template-columns:1fr}.composer{grid-template-columns:1fr}}
</style>
</head>
<body>
<div class="shell"><aside class="side"><div class="brand"><div class="logo">H</div><div><h1>Hanna V6.4</h1><p>Panel integrado · Chat · Motores · HP Mini</p></div></div><div class="nav">
<button class="active" onclick="tab('inicio',this)">Inicio / Salud</button>
<button onclick="tab('chat',this)">Chat online integrado</button>
<button onclick="tab('motores',this)">Motores y fases</button>
<button onclick="tab('funciones',this)">Funciones ON/OFF</button>
<button onclick="tab('memoria',this)">Memoria / Offline</button>
<button onclick="tab('skills',this)">Skills web</button>
<button onclick="tab('archivos',this)">Archivos</button>
<button onclick="tab('voz',this)">Voz / TTS / F8-F9</button>
<button onclick="tab('accesos',this)">Acceso móvil/JWT</button>
<button onclick="tab('sistema',this)">Sistema</button>
</div><p class="muted">El chat ya no está separado. Se usa desde este panel por la API móvil local.</p></aside><main class="main">
<section class="top"><div class="card metric"><small>Motor actual</small><div class="big" id="mEngine">...</div></div><div class="card metric"><small>Ollama</small><div class="big" id="mOllama">...</div></div><div class="card metric"><small>API móvil</small><div class="big" id="mMobile">...</div></div><div class="card metric"><small>Memoria</small><div class="big" id="mMemory">...</div></div></section>
<section id="inicio" class="tab"><div class="grid"><div class="card"><h2>Estado de Hanna</h2><p class="muted">Vista rediseñada para revisar de un golpe si Hanna arrancó completo después de MongoDB.</p><div id="healthPills"></div><pre id="status">Cargando...</pre><button class="primary" onclick="loadAll()">Actualizar</button><button class="secondary" onclick="openFolder('base')">Abrir proyecto</button><button class="secondary" onclick="openFolder('outputs')">Abrir salidas</button></div><div class="card"><h2>Arranque V6.4</h2><p class="muted">Los módulos nuevos no deben bloquear el arranque. Si memoria, auditoría, WebChat standalone o mantenimiento fallan, Hanna debe seguir levantando API móvil y panel.</p><pre>Orden esperado:
Ollama → MongoDB → servicios base → hotkeys → Mobile API 8790 → Admin Web 8787 → Telegram → listo.</pre><button class="secondary" onclick="post('/api/open-browser',{})">Abrir panel en navegador</button><button class="danger" onclick="restartHanna()">Reiniciar Hanna</button></div><div class="card"><h2>Acciones rápidas</h2><button class="primary" onclick="quick('Hanna estado del sistema')">Estado</button><button class="secondary" onclick="quick('Hanna usa Ollama')">Usar Ollama</button><button class="secondary" onclick="quick('Hanna usa Gemini')">Usar Gemini</button><button class="secondary" onclick="quick('Hanna abre Netflix en la PC y busca Dark')">Netflix Dark</button><button class="secondary" onclick="quick('Hanna busca en memoria lo que hicimos ayer')">Buscar memoria</button></div></div></section>
<section id="chat" class="tab hidden"><div class="two"><div class="card"><h2>Chat online integrado</h2><p class="muted">Este chat vive dentro del panel principal 8787. Usa la API móvil local 8790, no el servidor separado 8789.</p><label>Chat ID permitido</label><input id="chatId" placeholder="5112232887"><label>Pairing token</label><input id="pairToken" type="password" placeholder="HANNA_MOBILE_API_PAIRING_TOKEN"><div class="two"><div><label>Motor</label><select id="chatEngine"></select></div><div><label>Fase</label><select id="chatPhase"></select></div></div><button class="primary" onclick="saveChat()">Guardar conexión</button><button class="secondary" onclick="applyEngine()">Aplicar motor</button><button class="secondary" onclick="applyPhase()">Aplicar fase</button><pre id="chatState">Cargando...</pre></div><div class="card"><h2>Conversación</h2><div id="chatLog" class="chatBox"></div><div class="composer"><textarea id="chatInput" placeholder="Escribe para hablar con Hanna..."></textarea><button class="primary" onclick="sendChat()">Enviar</button></div></div></div></section>
<section id="motores" class="tab hidden"><div class="grid"><div class="card"><h2>Motor estricto</h2><p class="muted">Gemini, Groq, Ollama y OpenRouter deben respetarse sin salto cruzado si HANNA_ENGINE_ALLOW_CROSS_FALLBACK=false.</p><label>Motor</label><select id="engine"></select><button class="primary" onclick="setAdminEngine()">Aplicar motor</button><pre id="engineOut"></pre></div><div class="card"><h2>Fase activa</h2><p class="muted">La fase guía el comportamiento: local, ahorro, programación, multimedia, operaciones, estudio, nube o arquitectura.</p><label>Fase</label><select id="phase"></select><button class="primary" onclick="setAdminPhase()">Aplicar fase</button><pre id="phaseOut"></pre></div><div class="card"><h2>Configuración crítica</h2><pre id="criticalConfig"></pre></div></div></section>
<section id="funciones" class="tab hidden"><div class="card"><h2>Funciones ON/OFF</h2><p class="muted">Algunas opciones requieren reiniciar. Para HP Mini, evita wake word permanente y visión si no la usas.</p><div id="switchList"></div></div></section>
<section id="memoria" class="tab hidden"><div class="grid"><div class="card"><h2>Memoria jerárquica</h2><p class="muted">Resumen diario, índice local, index.db y auditoría. Debe permitir búsqueda offline cuando no haya internet.</p><pre id="memoryStatus"></pre><label>Buscar en memoria</label><input id="memoryQuery" placeholder="Ej. proyecto, spotify, error, mysql"><button class="primary" onclick="searchMemory()">Buscar</button><pre id="memoryOut"></pre></div><div class="card"><h2>Backup / Google Drive</h2><p class="muted">Requiere rclone configurado fuera de Hanna. Aquí se muestra si el remote y rutas están declarados.</p><pre id="backupStatus"></pre></div></div></section>
<section id="skills" class="tab hidden"><div class="card"><h2>Skills web</h2><p class="muted">Agregar comandos sin tocar código. Útil para Netflix, TV LG, apps, respuestas rápidas y generación de archivos.</p><button class="primary" onclick="newSkill()">Nueva skill</button><button class="secondary" onclick="saveSkills()">Guardar skills</button><div id="skillsList"></div></div></section>
<section id="archivos" class="tab hidden"><div class="two"><div class="card"><h2>Explorador</h2><select id="fileRoot"><option value="base">Proyecto</option><option value="hanna">Hanna personalidad</option><option value="profiles">Perfiles</option><option value="self">Self knowledge</option><option value="projects">Proyectos</option><option value="outputs">Salidas</option><option value="notebooks">Cuadernos</option><option value="context">Contexto</option></select><button class="primary" onclick="listFiles()">Listar</button><div id="fileList" class="files"></div></div><div class="card"><h2>Editor</h2><label>Ruta</label><input id="filePath"><textarea id="fileContent"></textarea><button class="primary" onclick="saveFile()">Guardar archivo</button></div></div></section>
<section id="voz" class="tab hidden"><div class="grid"><div class="card"><h2>Voz y TTS</h2><p class="muted">Para máxima velocidad usa texto. Para voz usa TTS en segundo plano.</p><label>Voz principal</label><input id="ttsVoice"><label>Voz respaldo</label><input id="ttsFallback"><button class="primary" onclick="saveVoice()">Guardar voz</button><button class="secondary" onclick="post('/api/test-tts',{text:'Hola, soy Hanna. Prueba de voz desde el panel integrado.'})">Probar TTS</button></div><div class="card"><h2>F8 / F9 / Overlay</h2><label>Overlay segundos</label><input id="overlaySeconds" type="number"><label>Silencio ms</label><input id="silenceMs" type="number"><label>RMS inicio</label><input id="startRms" type="number"><label>RMS final</label><input id="stopRms" type="number"><button class="primary" onclick="saveVoice()">Guardar</button></div></div></section>
<section id="accesos" class="tab hidden"><div class="grid"><div class="card"><h2>JWT móvil</h2><label>Chat ID</label><input id="jwtChatId"><label>Pairing token</label><input id="jwtPair" type="password"><button class="primary" onclick="createJwt()">Generar JWT</button><textarea id="jwtOut"></textarea></div><div class="card"><h2>Estado accesos</h2><pre id="accessStatus"></pre></div></div></section>
<section id="sistema" class="tab hidden"><div class="grid"><div class="card"><h2>Herramientas</h2><pre id="tools"></pre></div><div class="card"><h2>Google / móvil</h2><pre id="googleStatus"></pre></div><div class="card"><h2>Comandos</h2><div id="commands"></div></div></div></section>
</main></div><div id="toast" class="toast"></div>
<script>
let state={},skills=[],assignments=[];const $=id=>document.getElementById(id);function toast(t){let e=$('toast');e.textContent=t;e.style.display='block';setTimeout(()=>e.style.display='none',2600)}function tab(id,b){document.querySelectorAll('.tab').forEach(x=>x.classList.add('hidden'));$(id).classList.remove('hidden');document.querySelectorAll('.nav button').forEach(x=>x.classList.remove('active'));b?.classList.add('active')}async function api(p){let r=await fetch(p);let txt=await r.text();try{return JSON.parse(txt)}catch{return txt}}async function post(p,o){let r=await fetch(p,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(o||{})});let txt=await r.text();let d;try{d=JSON.parse(txt)}catch{d=txt}if(!r.ok)throw new Error(typeof d==='string'?d:(d.error||JSON.stringify(d)));return d}function esc(x){return String(x||'').replace(/[&<>\"]/g,m=>({'&':'&amp;','<':'&lt;','>':'&gt;','\"':'&quot;'}[m]))}
function pill(name,ok,extra=''){return `<span class="pill ${ok?'good':'bad'}">${name}: ${ok?'OK':'NO'}${extra?` · ${extra}`:''}</span>`}function renderHealth(){let s=state.status||{},set=state.settings||{};$('mEngine').textContent=state.engine||'...';$('mOllama').textContent=s.ollamaAvailable?'OK':'NO';$('mOllama').className='big '+(s.ollamaAvailable?'good':'bad');$('mMobile').textContent=set.mobileApiEnabled?'ON':'OFF';$('mMemory').textContent=s.tieredMemoryDbExists?'DB':'JSON';$('healthPills').innerHTML=[pill('Ollama',s.ollamaAvailable),pill('Mobile API',set.mobileApiEnabled,s.mobileApi),pill('Admin Web',set.adminWebEnabled,'8787'),pill('Chat integrado',true,'panel'),pill('Memoria DB',s.tieredMemoryDbExists),pill('Auditoría',s.auditLogEnabled),pill('RBAC',s.rbacEnabled),pill('Mantenimiento',s.nightlyMaintenanceEnabled),pill('Mongo',s.mongoEnabled,s.mongoDatabase)].join(' ');$('status').textContent=JSON.stringify(s,null,2);$('criticalConfig').textContent=JSON.stringify({engine:state.engine,activePhase:s.activePhase,webChatStandaloneEnabled:s.webChatStandaloneEnabled,tieredMemoryDbPath:s.tieredMemoryDbPath,auditLogPath:s.auditLogPath,backupRemote:s.backupRemote,screenAnalysisEnabled:set.screenAnalysisEnabled,wakeWordEnabled:set.wakeWordEnabled,ttsVoice:set.ttsVoice},null,2);$('memoryStatus').textContent=JSON.stringify({enabled:s.tieredMemoryEnabled,root:s.tieredMemoryRoot,indexDb:s.tieredMemoryDbPath,dbExists:s.tieredMemoryDbExists,audit:s.auditLogPath,auditExists:s.auditLogExists},null,2);$('backupStatus').textContent=JSON.stringify({nightly:s.nightlyMaintenanceEnabled,remote:s.backupRemote,rclone:'requiere rclone config externo',zstd:'requiere zstd instalado'},null,2)}
async function loadAll(){try{state=await api('/api/state');renderHealth();fillSelectors();fillSettings(state.settings||{});renderSwitches(state.settings||{});$('commands').innerHTML=(state.commands||[]).map(c=>`<p><b>${esc(c.comando)}</b><br><span class=muted>${esc(c.descripcion)}</span></p>`).join('');$('tools').textContent=JSON.stringify(await api('/api/tools'),null,2);$('googleStatus').textContent=JSON.stringify(await api('/api/google'),null,2);let a=await api('/api/access/status');$('accessStatus').textContent=JSON.stringify(a,null,2);$('jwtChatId').value=a.localChatId||'';skills=await api('/api/skills');renderSkills();await loadChatConfig();}catch(e){toast('Error cargando panel: '+e.message)}}
async function loadChatConfig(){let cfg=await api('/api/panel-chat/config');let saved=JSON.parse(localStorage.getItem('hanna_panel_chat_v64')||'{}');let engines=cfg.engines||[], phases=cfg.phases||[];for(const id of ['chatEngine','engine'])$(id).innerHTML=engines.map(e=>`<option value="${e.id}">${e.label}</option>`).join('');for(const id of ['chatPhase','phase'])$(id).innerHTML=phases.map(p=>`<option value="${p.id}">${p.label}</option>`).join('');$('chatId').value=saved.chatId||cfg.localChatId||'';$('pairToken').value=saved.pairingToken||'';$('chatEngine').value=saved.engine||'ollama';$('engine').value=saved.engine||'ollama';$('chatPhase').value=saved.phase||'local';$('phase').value=saved.phase||'local';$('chatState').textContent='API móvil: '+cfg.mobileApiBase+'\nChat integrado en panel 8787. Standalone 8789 no es necesario.';bubble('Chat integrado listo. Guarda el pairing token una vez.','sys')}function chatCfg(){return{chatId:$('chatId').value.trim(),pairingToken:$('pairToken').value.trim(),engine:$('chatEngine').value,phase:$('chatPhase').value}}function saveChat(){localStorage.setItem('hanna_panel_chat_v64',JSON.stringify(chatCfg()));toast('Conexión de chat guardada')}async function mobilePost(path,payload){let base=(state.status?.mobileApi||'http://127.0.0.1:8790').replace(/\/$/,'');let r=await fetch(base+path,{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify(payload)});let txt=await r.text();let d;try{d=JSON.parse(txt)}catch{d=txt}if(!r.ok)throw new Error(typeof d==='string'?d:(d.error||JSON.stringify(d)));return d}function bubble(t,c){let box=$('chatLog');if(!box)return;let d=document.createElement('div');d.className='bubble '+c;d.textContent=typeof t==='string'?t:JSON.stringify(t,null,2);box.appendChild(d);box.scrollTop=box.scrollHeight}async function applyEngine(){let c=chatCfg();saveChat();let r=await mobilePost('/api/mobile/engine',{engine:c.engine,mode:c.engine,motor:c.engine,pairingToken:c.pairingToken,token:c.pairingToken});$('chatState').textContent=JSON.stringify(r,null,2);await post('/api/engine',{mode:c.engine});await loadAll()}async function applyPhase(){let c=chatCfg();saveChat();let r=await mobilePost('/api/mobile/phase',{phase:c.phase,fase:c.phase,pairingToken:c.pairingToken,token:c.pairingToken});$('chatState').textContent=JSON.stringify(r,null,2);await post('/api/phase',{phase:c.phase});await loadAll()}async function sendChat(){let msg=$('chatInput').value.trim();if(!msg)return;$('chatInput').value='';bubble(msg,'user');let c=chatCfg();saveChat();try{let r=await mobilePost('/api/mobile/message',{chatId:c.chatId,chat_id:c.chatId,message:msg,text:msg,engine:c.engine,motor:c.engine,phase:c.phase,fase:c.phase,pairingToken:c.pairingToken,token:c.pairingToken,source:'admin-panel-v6.4'});bubble(r.response||r.message||r.text||r,'bot')}catch(e){bubble('Error: '+e.message,'sys')}}function quick(m){tab('chat',document.querySelector('.nav button:nth-child(2)'));$('chatInput').value=m;sendChat()}
function fillSelectors(){if($('engine')?.options?.length){$('engine').value=(state.engine||'ollama').toLowerCase().includes('gemini')?'gemini':(state.engine||'ollama').toLowerCase().includes('groq')?'groq':(state.engine||'ollama').toLowerCase().includes('openrouter')?'openrouter':(state.engine||'ollama').toLowerCase().includes('hybrid')?'hybrid':'ollama'}}async function setAdminEngine(){let v=$('engine').value;let r=await post('/api/engine',{mode:v,engine:v,motor:v});$('engineOut').textContent=JSON.stringify(r,null,2);if($('chatEngine'))$('chatEngine').value=v;await loadAll()}async function setAdminPhase(){let v=$('phase').value;let r=await post('/api/phase',{phase:v,fase:v});$('phaseOut').textContent=JSON.stringify(r,null,2);if($('chatPhase'))$('chatPhase').value=v;await loadAll()}async function searchMemory(){let c=chatCfg();let q=$('memoryQuery').value.trim();if(!q)return;try{let r=await mobilePost('/api/mobile/memory/search',{query:q,limit:5,pairingToken:c.pairingToken,token:c.pairingToken});$('memoryOut').textContent=JSON.stringify(r,null,2)}catch(e){$('memoryOut').textContent='Error: '+e.message}}
function bools(){return[['adminWebEnabled','Panel web','Servidor local 8787'],['mobileApiEnabled','API móvil','Chat integrado y futura app móvil'],['wakeWordEnabled','Wake word','Escucha permanente Hanna'],['dynamicSkillsEnabled','Skills dinámicas','Crear comandos desde web'],['assignmentsEnabled','Tareas','Recordatorios y cuadernos'],['googleIntegrationEnabled','Google','Classroom/Drive/Calendar'],['screenAnalysisEnabled','F9 pantalla','Análisis de pantalla'],['mirrorLocalToTelegram','Replicar a Telegram','Enviar copia al dueño'],['overlayEnabled','Overlay','Ventana flotante'],['localHotkeyEnabled','Hotkeys','F8/AltGr/F9'],['agentOpenGeneratedCode','Abrir código generado','Para HP Mini mejor desactivado'],['localVoiceCameraLedEnabled','LED cámara','Indicador visual de voz'],['voiceRecordImmediately','Grabar inmediato','No perder primeras palabras'],['ollamaAutoStart','Auto iniciar Ollama','Levanta Ollama local'],['startupGreetingEnabled','Saludo inicial','Para segundo plano mejor apagado'],['preferLocalForComputer','PC usa local','Prioriza Ollama'],['preferHybridForTelegram','Telegram híbrido','Usar nubes si se permite']]}function renderSwitches(s){$('switchList').innerHTML=bools().map(([k,t,d])=>`<label class="switch"><span><b>${t}</b><br><small class="muted">${d}</small></span><input class="toggle" type="checkbox" ${s[k]?'checked':''} onchange="toggleSetting('${k}',this.checked)"></label>`).join('')}async function toggleSetting(k,v){let r=await post('/api/settings/toggle',{key:k,enabled:v});toast(r.result||'Guardado');await loadAll()}function fillSettings(s){$('ttsVoice').value=s.ttsVoice||'';$('ttsFallback').value=s.ttsFallbackVoice||'';$('overlaySeconds').value=s.overlaySeconds||12;$('silenceMs').value=s.localVoiceSilenceMs||1800;$('startRms').value=s.localVoiceStartRms||300;$('stopRms').value=s.localVoiceStopRms||180}async function saveVoice(){let s=state.settings||{};s.ttsVoice=$('ttsVoice').value;s.ttsFallbackVoice=$('ttsFallback').value;s.overlaySeconds=+$('overlaySeconds').value;s.localVoiceSilenceMs=+$('silenceMs').value;s.localVoiceStartRms=+$('startRms').value;s.localVoiceStopRms=+$('stopRms').value;await post('/api/settings',s);toast('Voz guardada');await loadAll()}function openFolder(k){post('/api/open-folder',{kind:k}).catch(e=>toast(e.message))}async function restartHanna(){if(confirm('¿Reiniciar Hanna?'))await post('/api/restart',{})}
function renderSkills(){skills=skills||[];$('skillsList').innerHTML=skills.map((s,i)=>`<div class="card"><label><input type="checkbox" ${s.enabled?'checked':''} onchange="skills[${i}].enabled=this.checked"> Activa</label><label>Nombre</label><input value="${esc(s.name)}" onchange="skills[${i}].name=this.value"><label>Disparadores separados por coma</label><input value="${esc((s.triggers||[]).join(','))}" onchange="skills[${i}].triggers=this.value.split(',').map(x=>x.trim()).filter(Boolean)"><label>Acción</label><select onchange="skills[${i}].actionType=this.value"><option ${sel(s.actionType,'reply')}>reply</option><option ${sel(s.actionType,'open_app')}>open_app</option><option ${sel(s.actionType,'open_url')}>open_url</option><option ${sel(s.actionType,'generate_code')}>generate_code</option><option ${sel(s.actionType,'notebook')}>notebook</option></select><label>Payload</label><textarea onchange="skills[${i}].payload=this.value">${esc(s.payload||'')}</textarea></div>`).join('')}function sel(a,b){return String(a||'reply')==b?'selected':''}function newSkill(){skills.push({id:'',enabled:true,name:'Nueva skill',triggers:[''],actionType:'reply',payload:'Respuesta de Hanna'});renderSkills()}async function saveSkills(){await post('/api/skills/all',skills);toast('Skills guardadas');await loadAll()}async function listFiles(){let d=await api('/api/files?root='+encodeURIComponent($('fileRoot').value));$('fileList').innerHTML=[...(d.folders||[]).map(f=>`📁 ${esc(f.name)}`),...(d.files||[]).map(f=>`<a href="#" onclick="readFile('${encodeURIComponent(f.path)}')">📄 ${esc(f.name)}</a>`)].join('')}async function readFile(p){let d=await api('/api/file?path='+p);if(d.ok){$('filePath').value=d.path;$('fileContent').value=d.content}}async function saveFile(){await post('/api/file',{path:$('filePath').value,content:$('fileContent').value});toast('Archivo guardado')}async function createJwt(){let r=await post('/api/access/token',{telegramChatId:$('jwtChatId').value,pairingToken:$('jwtPair').value});$('jwtOut').value=r.authorization||JSON.stringify(r,null,2)}
loadAll();
</script>
</body>
</html>
""";


    private static string GetUpdatedCommandsHtml()
    {
        return "<pre style='white-space:pre-wrap'>" + CommandCatalogService.GetCommandsText() + "</pre>";
    }
}
