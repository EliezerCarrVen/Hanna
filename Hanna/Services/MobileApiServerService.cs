using Hanna.Core;
using Hanna.Models;
using Hanna.Skills;
using System.Net;
using System.Net.Sockets;
using Telegram.Bot;

namespace Hanna.Services;

internal sealed class MobileApiServerService : IDisposable
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService runtime;
    private readonly SkillRouter skillRouter;
    private readonly TelegramBotClient botClient;
    private readonly JwtTokenService jwt;
    private readonly ModelModeService modelMode;
    private readonly PhaseService phaseService;
    private readonly TieredMemoryService tieredMemory;
    private readonly AuditTrailService audit;
    private readonly CommandAuthorizationService authorization;
    private TcpListener? listener;
    private CancellationTokenSource? cts;
    private Task? loopTask;

    public MobileApiServerService(AppConfig config, RuntimeSettingsService runtime, SkillRouter skillRouter, TelegramBotClient botClient, ModelModeService modelMode, PhaseService phaseService, TieredMemoryService tieredMemory, AuditTrailService audit, CommandAuthorizationService authorization)
    {
        this.config = config;
        this.runtime = runtime;
        this.skillRouter = skillRouter;
        this.botClient = botClient;
        this.modelMode = modelMode;
        this.phaseService = phaseService;
        this.tieredMemory = tieredMemory;
        this.audit = audit;
        this.authorization = authorization;
        jwt = new JwtTokenService(config);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!config.MobileApiEnabled)
            return Task.CompletedTask;

        try
        {
            IPAddress ip = config.MobileApiBindHost.Equals("0.0.0.0", StringComparison.OrdinalIgnoreCase)
                ? IPAddress.Any
                : IPAddress.Parse(config.MobileApiBindHost);

            listener = new TcpListener(ip, Math.Clamp(config.MobileApiPort, 1024, 65535));
            listener.Start();
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            loopTask = Task.Run(() => AcceptLoop(cts.Token));
            Console.WriteLine($"[Mobile API] Activa en http://{config.MobileApiBindHost}:{config.MobileApiPort}/");
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Mobile API Error]: " + ex.Message);
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
                _ = Task.Run(() => Handle(client, token));
            }
            catch { client?.Dispose(); }
        }
    }

    private async Task Handle(TcpClient client, CancellationToken token)
    {
        await using NetworkStream stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        try
        {
            string? requestLine = await reader.ReadLineAsync(token);
            if (string.IsNullOrWhiteSpace(requestLine)) return;
            string[] parts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
            string method = parts[0].ToUpperInvariant();
            string path = parts.Length > 1 ? parts[1].Split('?', 2)[0] : "/";
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            while (true)
            {
                string? line = await reader.ReadLineAsync(token);
                if (string.IsNullOrEmpty(line)) break;
                int idx = line.IndexOf(':');
                if (idx > 0) headers[line[..idx].Trim()] = line[(idx + 1)..].Trim();
            }
            string body = "";
            if (headers.TryGetValue("Content-Length", out string? lenText) && int.TryParse(lenText, out int len) && len > 0)
            {
                char[] buffer = new char[len];
                int read = 0;
                while (read < len)
                {
                    int current = await reader.ReadAsync(buffer.AsMemory(read, len - read), token);
                    if (current <= 0) break;
                    read += current;
                }
                body = new string(buffer, 0, read);
            }

            if (method == "OPTIONS")
            {
                await WriteJson(stream, 200, new { ok = true }, token);
                return;
            }

            if (method == "GET" && path == "/api/mobile/state")
            {
                await WriteJson(stream, 200, new
                {
                    ok = true,
                    name = "Hanna",
                    model = runtime.Snapshot().OllamaModel,
                    time = DateTimeOffset.Now,
                    jwtConfigured = jwt.IsConfigured,
                    pairingConfigured = !string.IsNullOrWhiteSpace(config.MobileApiPairingToken),
                    requiresJwt = true,
                    endpoints = new[]
                    {
                        "POST /api/mobile/auth/telegram-login",
                        "GET /api/mobile/auth/status",
                        "POST /api/mobile/message",
                        "GET /api/mobile/engines",
                        "POST /api/mobile/engine",
                        "GET /api/mobile/phases",
                        "POST /api/mobile/phase",
                        "POST /api/mobile/memory/search"
                    }
                }, token);
                return;
            }

            if (method == "GET" && path == "/api/mobile/auth/status")
            {
                JwtValidationResult validation = ValidateAuthorization(headers);
                await WriteJson(stream, validation.IsValid ? 200 : 401, new
                {
                    ok = validation.IsValid,
                    chatId = validation.ChatId,
                    role = validation.Role,
                    displayName = validation.DisplayName,
                    expiresAt = validation.ExpiresAt,
                    error = validation.Error
                }, token);
                return;
            }

            if (method == "POST" && path == "/api/mobile/auth/telegram-login")
            {
                using JsonDocument doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                string pairingToken = ReadString(doc, "pairingToken", "token");
                if (!IsPairingTokenValid(pairingToken))
                {
                    await WriteJson(stream, 401, new { ok = false, error = "Pairing token inválido" }, token);
                    return;
                }

                string chatIdText = ReadString(doc, "telegramChatId", "telegram_chat_id", "chatId");
                if (!long.TryParse(chatIdText, out long chatId) || !IsChatAllowed(chatId))
                {
                    await WriteJson(stream, 403, new { ok = false, error = "ChatId no permitido" }, token);
                    return;
                }

                if (!jwt.IsConfigured)
                {
                    await WriteJson(stream, 500, new { ok = false, error = "Configura HANNA_JWT_SECRET en HannaEnv.env" }, token);
                    return;
                }

                string accessToken = jwt.CreateMobileToken(chatId, chatId == config.LocalChatId ? "dueno" : "usuario", "Hanna Mobile");
                await WriteJson(stream, 200, new
                {
                    ok = true,
                    access_token = accessToken,
                    token_type = "bearer",
                    chatId,
                    use = "Authorization: Bearer " + accessToken
                }, token);
                return;
            }


            if (method == "GET" && path == "/api/mobile/engines")
            {
                await WriteJson(stream, 200, new
                {
                    ok = true,
                    current = modelMode.GetModeLabel(),
                    allowCrossFallback = config.AllowCrossEngineFallback,
                    engines = new[]
                    {
                        new { id = "ollama", label = "Ollama local" },
                        new { id = "gemini", label = "Gemini directo" },
                        new { id = "groq", label = "Groq directo" },
                        new { id = "openrouter", label = "OpenRouter" },
                        new { id = "hybrid", label = "Híbrido" },
                        new { id = "original", label = "Original" }
                    }
                }, token);
                return;
            }

            if (method == "POST" && path == "/api/mobile/engine")
            {
                using JsonDocument doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                JwtValidationResult validation = ValidateAuthorization(headers);
                if (!validation.IsValid)
                {
                    string legacyToken = ReadString(doc, "token", "pairingToken");
                    if (!IsPairingTokenValid(legacyToken))
                    {
                        await WriteJson(stream, 401, new { ok = false, error = "JWT o pairing token inválido", jwtError = validation.Error }, token);
                        return;
                    }
                }
                long chatId = validation.IsValid ? validation.ChatId : config.LocalChatId;
                string role = validation.IsValid ? validation.Role : "dueno";
                string engine = ReadString(doc, "engine", "motor", "mode");
                if (!authorization.CanExecute(chatId, role, "engine", engine, out string reason))
                {
                    await audit.AppendAsync(chatId, "MobileApi", "engine_denied", engine, false, token);
                    await WriteJson(stream, 403, new { ok = false, error = reason }, token);
                    return;
                }
                EngineMode parsed = ModelModeService.ParseFromText("Hanna usa " + engine);
                await modelMode.SetMode(parsed, token);
                await WriteJson(stream, 200, new { ok = true, engine = modelMode.GetModeLabel(), allowCrossFallback = config.AllowCrossEngineFallback }, token);
                return;
            }

            if (method == "GET" && path == "/api/mobile/phases")
            {
                await WriteJson(stream, 200, new
                {
                    ok = true,
                    current = phaseService.GetActivePhase(),
                    phases = phaseService.ListPhases()
                }, token);
                return;
            }

            if (method == "POST" && path == "/api/mobile/phase")
            {
                using JsonDocument doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                JwtValidationResult validation = ValidateAuthorization(headers);
                if (!validation.IsValid)
                {
                    string legacyToken = ReadString(doc, "token", "pairingToken");
                    if (!IsPairingTokenValid(legacyToken))
                    {
                        await WriteJson(stream, 401, new { ok = false, error = "JWT o pairing token inválido", jwtError = validation.Error }, token);
                        return;
                    }
                }

                long chatId = validation.IsValid ? validation.ChatId : config.LocalChatId;
                string role = validation.IsValid ? validation.Role : "dueno";
                string phase = ReadString(doc, "phase", "fase");
                if (!authorization.CanExecute(chatId, role, "phase", phase, out string reason))
                {
                    await audit.AppendAsync(chatId, "MobileApi", "phase_denied", phase, false, token);
                    await WriteJson(stream, 403, new { ok = false, error = reason }, token);
                    return;
                }
                phase = await phaseService.SetActivePhase(phase, token);
                await audit.AppendAsync(chatId, "MobileApi", "phase", phase, true, token);
                await WriteJson(stream, 200, new { ok = true, phase }, token);
                return;
            }

            if (method == "POST" && path == "/api/mobile/memory/search")
            {
                using JsonDocument doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                JwtValidationResult validation = ValidateAuthorization(headers);
                if (!validation.IsValid)
                {
                    string legacyToken = ReadString(doc, "token", "pairingToken");
                    if (!IsPairingTokenValid(legacyToken))
                    {
                        await WriteJson(stream, 401, new { ok = false, error = "JWT o pairing token inválido", jwtError = validation.Error }, token);
                        return;
                    }
                }

                string query = ReadString(doc, "query", "text", "message");
                string limitText = ReadString(doc, "limit");
                int limit = int.TryParse(limitText, out int parsedLimit) ? Math.Clamp(parsedLimit, 1, 10) : 5;
                await WriteJson(stream, 200, new { ok = true, results = tieredMemory.Search(query, limit) }, token);
                return;
            }

            if (method == "POST" && path == "/api/mobile/message")
            {
                using JsonDocument doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(body) ? "{}" : body);
                JwtValidationResult validation = ValidateAuthorization(headers);

                if (!validation.IsValid)
                {
                    string legacyToken = ReadString(doc, "token", "pairingToken");
                    if (!IsPairingTokenValid(legacyToken))
                    {
                        await WriteJson(stream, 401, new { ok = false, error = "JWT o pairing token inválido", jwtError = validation.Error }, token);
                        return;
                    }
                }

                long chatId = validation.IsValid && validation.ChatId > 0 ? validation.ChatId : config.LocalChatId;
                string role = validation.IsValid ? validation.Role : "dueno";
                string text = ReadString(doc, "text", "message", "prompt");
                if (!authorization.CanExecute(chatId, role, "message", text, out string reason))
                {
                    await audit.AppendAsync(chatId, "MobileApi", "message_denied", text, false, token);
                    await WriteJson(stream, 403, new { ok = false, error = reason }, token);
                    return;
                }
                if (string.IsNullOrWhiteSpace(text))
                {
                    await WriteJson(stream, 400, new { ok = false, error = "Falta text" }, token);
                    return;
                }

                SkillResult result = await skillRouter.Route(chatId, text, botClient, token);
                await WriteJson(stream, 200, new { ok = true, response = result.ResponseText, handled = result.Handled, chatId }, token);
                return;
            }

            await WriteJson(stream, 404, new { ok = false, error = "Ruta no encontrada" }, token);
        }
        catch (Exception ex)
        {
            await WriteJson(stream, 500, new { ok = false, error = ex.Message }, token);
        }
        finally { client.Dispose(); }
    }

    private JwtValidationResult ValidateAuthorization(Dictionary<string, string> headers)
    {
        if (!headers.TryGetValue("Authorization", out string? auth))
            return JwtValidationResult.Fail("Falta Authorization");
        return jwt.Validate(auth);
    }

    private bool IsPairingTokenValid(string token)
    {
        return !string.IsNullOrWhiteSpace(config.MobileApiPairingToken) && token == config.MobileApiPairingToken;
    }

    private bool IsChatAllowed(long chatId)
    {
        return chatId == config.LocalChatId || config.AllowedChats.Contains(chatId);
    }

    private static string ReadString(JsonDocument doc, params string[] names)
    {
        foreach (string name in names)
        {
            if (doc.RootElement.TryGetProperty(name, out JsonElement el))
                return el.ValueKind == JsonValueKind.String ? el.GetString() ?? "" : el.ToString();
        }
        return "";
    }

    private static async Task WriteJson(Stream stream, int status, object value, CancellationToken token)
    {
        byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true }));
        string statusText = status switch { 200 => "OK", 400 => "Bad Request", 401 => "Unauthorized", 403 => "Forbidden", 404 => "Not Found", 500 => "Internal Server Error", _ => "OK" };
        string header = $"HTTP/1.1 {status} {statusText}\r\nContent-Type: application/json; charset=utf-8\r\nAccess-Control-Allow-Origin: *\r\nAccess-Control-Allow-Headers: Authorization, Content-Type\r\nAccess-Control-Allow-Methods: GET, POST, OPTIONS\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n";
        await stream.WriteAsync(Encoding.ASCII.GetBytes(header), token);
        await stream.WriteAsync(body, token);
    }

    public void Dispose()
    {
        try { cts?.Cancel(); } catch { }
        try { listener?.Stop(); } catch { }
    }
}
