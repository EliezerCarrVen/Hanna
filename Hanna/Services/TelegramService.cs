using Hanna.Core;
using System.Text.RegularExpressions;
using Hanna.Models;
using Hanna.Skills;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Hanna.Services;

internal sealed partial class TelegramService
{
    private readonly AppConfig config;
    private readonly ConversationLogService logs;
    private readonly ContextService context;
    private readonly ResponseService response;
    private readonly SkillRouter skillRouter;
    private readonly GroqService groq;
    private readonly VisionService vision;
    private readonly MongoLogService mongoLogs;
    private readonly ModelModeService modelMode;
    private TelegramBotClient? botClient;
    private readonly HttpClient httpClient = new();
    private readonly InterruptionManager interruptionManager = new();
    private readonly MultiCommandParser multiCommandParser = new();

    internal TelegramService(
        AppConfig config,
        ConversationLogService logs,
        ContextService context,
        ResponseService response,
        SkillRouter skillRouter,
        GroqService groq,
        VisionService vision,
        MongoLogService mongoLogs,
        ModelModeService modelMode)
    {
        this.config = config;
        this.logs = logs;
        this.context = context;
        this.response = response;
        this.skillRouter = skillRouter;
        this.groq = groq;
        this.vision = vision;
        this.mongoLogs = mongoLogs;
        this.modelMode = modelMode;
    }

    internal TelegramService(
        AppConfig config,
        ConversationLogService logs,
        ContextService context,
        ResponseService response,
        SkillRouter skillRouter,
        GroqService groq,
        VisionService vision,
        MongoLogService mongoLogs,
        ModelModeService modelMode,
        ContextArchiveService contextArchive)
        : this(config, logs, context, response, skillRouter, groq, vision, mongoLogs, modelMode)
    {
        _ = contextArchive;
    }

    public async Task StartAsync()
    {
        if (!IsValidTelegramToken(config.TelegramToken))
        {
            Console.WriteLine("[Telegram] TELEGRAM_TOKEN vacío, inválido o placeholder. Telegram queda desactivado; Hanna seguirá local.");
            return;
        }

        if (string.IsNullOrWhiteSpace(config.GroqApiKey))
        {
            Console.WriteLine("[Telegram] Falta GROQ_API_KEY. Telegram queda desactivado para evitar fallos en transcripción.");
            return;
        }

        try
        {
            botClient = new TelegramBotClient(config.TelegramToken);
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine("[Telegram] Token inválido: " + ex.Message);
            return;
        }

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, receiverOptions);

        await SendStartupGreeting();
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        if (botClient == null || update.Message is not { } message)
            return;

        long chatId = message.Chat.Id;
        string incomingText = message.Text?.Trim() ?? "";
        bool authorized = config.AllowedChats.Count == 0 || config.AllowedChats.Contains(chatId);

        await mongoLogs.UpsertUser(chatId, message.From, authorized, cancellationToken);

        await mongoLogs.RegisterConnection(
            chatId,
            "message_received",
            authorized ? "authorized" : "blocked",
            message.Type.ToString(),
            cancellationToken);

        if (IsMiIdCommand(incomingText))
        {
            await SendTextHttp(chatId, $"Tu chatId es: {chatId}", cancellationToken);
            return;
        }

        if (!authorized)
        {
            await SendTextHttp(
                chatId,
                $"No estás autorizado para usar este bot.\n\nTu chatId es: {chatId}\n\nPídele al dueño que agregue este ID en TELEGRAM_ALLOWED_CHAT_IDS.",
                cancellationToken);

            return;
        }

        string text = "";
        CancellationToken activeToken = cancellationToken;

        try
        {
            if (message.Voice != null)
            {
                await SendTextHttp(chatId, "Estoy escuchando tu audio...", cancellationToken);

                string? filePath = await GetFilePath(message.Voice.FileId, cancellationToken);

                if (string.IsNullOrWhiteSpace(filePath))
                {
                    await response.Send(botClient, chatId, "No pude obtener el audio desde Telegram.", cancellationToken);
                    return;
                }

                string temp = Path.Combine(Path.GetTempPath(), $"input_{chatId}_{Guid.NewGuid()}.ogg");

                await using (var fileStream = File.Open(temp, FileMode.Create))
                    await DownloadFile(filePath, fileStream, cancellationToken);

                text = await groq.TranscribeAudio(temp, cancellationToken);

                try
                {
                    File.Delete(temp);
                }
                catch
                {
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    await response.Send(botClient, chatId, "No pude entender el audio.", cancellationToken, true);
                    return;
                }

                text = text.Trim();

                PrintUserConsole(chatId, $"🎙️ Audio transcrito: {text}");

                await RegisterMessageSafe(chatId, "USUARIO", $"[Audio transcrito] {text}", cancellationToken);

                await mongoLogs.RegisterMessage(
                    chatId,
                    "USUARIO",
                    "voice",
                    text,
                    "",
                    response.GetMode(chatId),
                    ShadowModeService.IsShadowActive(chatId),
                    cancellationToken);
            }
            else if (message.Photo != null && message.Photo.Length > 0)
            {
                PrintUserConsole(chatId, string.IsNullOrWhiteSpace(message.Caption)
                    ? "🖼️ Imagen recibida."
                    : $"🖼️ Imagen recibida con texto: {message.Caption}");

                await HandlePhoto(message, chatId, cancellationToken);
                return;
            }
            else if (!string.IsNullOrWhiteSpace(message.Text))
            {
                text = message.Text.Trim();

                PrintUserConsole(chatId, text);

                await RegisterMessageSafe(chatId, "USUARIO", text, cancellationToken);

                await mongoLogs.RegisterMessage(
                    chatId,
                    "USUARIO",
                    "text",
                    text,
                    "",
                    response.GetMode(chatId),
                    ShadowModeService.IsShadowActive(chatId),
                    cancellationToken);
            }
            else
            {
                return;
            }

            if (multiCommandParser.IsStopCommand(text))
            {
                interruptionManager.Stop(chatId);
                await response.Send(botClient, chatId, "Me detengo.", CancellationToken.None, true);
                return;
            }

            activeToken = interruptionManager.Begin(chatId, cancellationToken);

            Console.WriteLine($"[Telegram] Enrutando mensaje con motor actual: {modelMode.GetModeLabel()}");
            SkillResult skillResult = await skillRouter.Route(chatId, text, botClient, activeToken);
            Console.WriteLine($"[Telegram] Resultado skill: handled={skillResult.Handled}, skip={skillResult.SkipResponse}, responseLen={(skillResult.ResponseText ?? "").Length}");

            if (!skillResult.Handled || (!skillResult.SkipResponse && string.IsNullOrWhiteSpace(skillResult.ResponseText)))
            {
                skillResult = SkillResult.Text("Te leÃ­, pero el motor no generÃ³ respuesta. Ya forcÃ© modo local con Ollama; intenta de nuevo o revisa la consola de Ollama.");
            }

            if (!skillResult.Handled)
                return;

            if (!skillResult.SkipResponse && !string.IsNullOrWhiteSpace(skillResult.ResponseText))
            {
                await response.Send(botClient, chatId, skillResult.ResponseText, cancellationToken, skillResult.ForceText);

                await RegisterMessageSafe(chatId, "HANNA", skillResult.ResponseText, cancellationToken);

                await mongoLogs.RegisterMessage(
                    chatId,
                    "HANNA",
                    "text",
                    skillResult.ResponseText,
                    "",
                    response.GetMode(chatId),
                    ShadowModeService.IsShadowActive(chatId),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            await RegisterMessageSafe(chatId, "SISTEMA", "Respuesta cancelada por una orden nueva o por 'Hanna para'.", CancellationToken.None);
        }
        catch (Exception ex)
        {
            if (!ShadowModeService.IsShadowActive(chatId))
                Console.WriteLine($"Error crítico: {ex}");

            await RegisterMessageSafe(chatId, "ERROR", ex.ToString(), cancellationToken);

            await mongoLogs.RegisterError(chatId, "TelegramService", ex, cancellationToken);

            await response.Send(
                botClient,
                chatId,
                "Tuve un error interno. Ya lo dejé registrado para revisarlo.",
                CancellationToken.None,
                true);
        }
    }

    private async Task HandlePhoto(Message message, long chatId, CancellationToken cancellationToken)
    {
        if (botClient == null || message.Photo == null)
            return;

        await SendTextHttp(chatId, "Analizando la imagen...", cancellationToken);

        var photo = message.Photo[^1];
        string? filePath = await GetFilePath(photo.FileId, cancellationToken);

        if (string.IsNullOrWhiteSpace(filePath))
        {
            await response.Send(botClient, chatId, "No pude obtener la imagen desde Telegram.", cancellationToken);
            return;
        }

        string temp = Path.Combine(Path.GetTempPath(), $"input_{chatId}_{Guid.NewGuid()}.jpg");

        try
        {
            await using (var stream = File.Open(temp, FileMode.Create))
                await DownloadFile(filePath, stream, cancellationToken);

            byte[] imageBytes = await File.ReadAllBytesAsync(temp, cancellationToken);
            string base64 = Convert.ToBase64String(imageBytes);

            string caption = message.Caption ?? "";

            string prompt =
                "Analiza la imagen y responde al usuario de forma natural como Hanna.\n" +
                "Si la imagen no es de música, describe solo lo que se ve.\n";

            if (!string.IsNullOrWhiteSpace(caption))
                prompt += "El usuario escribió junto con la imagen: " + caption;

            HannaContext hannaContext = await context.BuildContext(chatId, cancellationToken);
            string rawAnswer = await vision.AnalyzeWithGroq(prompt, base64, hannaContext, cancellationToken);

            string answer = CleanVisionInternalOutput(rawAnswer);
            string detectedMusic = ExtractDetectedMusic(rawAnswer);

            if (string.IsNullOrWhiteSpace(answer))
                answer = "Ya analicé la imagen, pero no encontré algo claro para describir.";

            if (!string.IsNullOrWhiteSpace(detectedMusic) && WantsMusicAction(caption))
            {
                string musicCommand = BuildMusicCommandFromCaption(caption, detectedMusic);
                SkillResult musicResult = await skillRouter.Route(chatId, musicCommand, botClient, cancellationToken);

                if (musicResult.Handled && !string.IsNullOrWhiteSpace(musicResult.ResponseText))
                    answer = answer + "\n\n" + musicResult.ResponseText;
            }

            await response.Send(botClient, chatId, answer, cancellationToken);

            await RegisterMessageSafe(chatId, "USUARIO", $"[Imagen] {caption}", cancellationToken);
            await RegisterMessageSafe(chatId, "HANNA", answer, cancellationToken);

            await mongoLogs.RegisterMessage(
                chatId,
                "USUARIO",
                "photo",
                caption,
                "",
                response.GetMode(chatId),
                ShadowModeService.IsShadowActive(chatId),
                cancellationToken);

            await mongoLogs.RegisterMessage(
                chatId,
                "HANNA",
                "text",
                answer,
                "GroqVision",
                response.GetMode(chatId),
                ShadowModeService.IsShadowActive(chatId),
                cancellationToken);
        }
        finally
        {
            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch
            {
            }
        }
    }


    private static string ExtractDetectedMusic(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        foreach (string line in raw.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None))
        {
            string clean = line.Trim();

            Match match = Regex.Match(clean, @"^(?:MUSICA_DETECTADA|CANCION_DETECTADA|CANCIÓN_DETECTADA|MÚSICA_DETECTADA)\s*:\s*(.+)$", RegexOptions.IgnoreCase);

            if (match.Success)
                return match.Groups[1].Value.Trim();
        }

        return "";
    }

    private static bool WantsMusicAction(string caption)
    {
        if (string.IsNullOrWhiteSpace(caption))
            return false;

        string clean = caption.ToLowerInvariant();

        return Regex.IsMatch(clean, @"\b(reproduce|pon|toca|agrega|añade|anade|guarda|playlist|favoritos|me gusta|liked)\b", RegexOptions.IgnoreCase);
    }

    private static string BuildMusicCommandFromCaption(string caption, string detectedMusic)
    {
        string clean = caption.ToLowerInvariant();

        if (Regex.IsMatch(clean, @"\b(agrega|añade|anade|guarda)\b") &&
            Regex.IsMatch(clean, @"\b(me gusta|favoritos|liked)\b"))
            return "guarda en me gusta " + detectedMusic;

        if (Regex.IsMatch(clean, @"\b(agrega|añade|anade)\b") &&
            Regex.IsMatch(clean, @"\b(fila|cola)\b"))
            return "agrega a la fila " + detectedMusic;

        if (Regex.IsMatch(clean, @"\b(agrega|añade|anade)\b") &&
            Regex.IsMatch(clean, @"\b(playlist)\b"))
            return caption + " " + detectedMusic;

        return "reproduce " + detectedMusic;
    }

    private static string CleanVisionInternalOutput(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "";

        var lines = raw
            .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
            .Where(line =>
            {
                string clean = line.Trim();

                if (string.IsNullOrWhiteSpace(clean))
                    return false;

                if (Regex.IsMatch(clean, @"^(MUSICA_DETECTADA|CANCION_DETECTADA|CANCIÓN_DETECTADA|MÚSICA_DETECTADA)\s*:", RegexOptions.IgnoreCase))
                    return false;

                return true;
            })
            .ToList();

        return string.Join(Environment.NewLine, lines).Trim();
    }

    private async Task<string?> GetFilePath(string fileId, CancellationToken cancellationToken)
    {
        string url = $"https://api.telegram.org/bot{config.TelegramToken}/getFile?file_id={Uri.EscapeDataString(fileId)}";

        using var resp = await httpClient.GetAsync(url, cancellationToken);

        if (!resp.IsSuccessStatusCode)
            return null;

        string json = await resp.Content.ReadAsStringAsync(cancellationToken);

        using JsonDocument doc = JsonDocument.Parse(json);

        return doc.RootElement.GetProperty("result").GetProperty("file_path").GetString();
    }

    private async Task DownloadFile(string filePath, Stream destination, CancellationToken cancellationToken)
    {
        string url = $"https://api.telegram.org/file/bot{config.TelegramToken}/{filePath}";

        using var resp = await httpClient.GetAsync(url, cancellationToken);

        resp.EnsureSuccessStatusCode();

        await resp.Content.CopyToAsync(destination, cancellationToken);
    }

    private async Task SendStartupGreeting()
    {
        if (botClient == null)
            return;

        if (!config.StartupGreetingEnabled || !config.StartupGreetingTelegramEnabled)
        {
            Console.WriteLine("[Telegram] Saludo inicial desactivado.");
            return;
        }

        long targetChatId = ResolveStartupGreetingChatId();

        if (targetChatId == 0)
        {
            Console.WriteLine("[Telegram] Saludo inicial omitido: no hay HANNA_LOCAL_CHAT_ID ni TELEGRAM_ALLOWED_CHAT_IDS válido.");
            return;
        }

        string greeting = string.IsNullOrWhiteSpace(config.StartupGreetingText)
            ? "Hanna está en línea."
            : config.StartupGreetingText.Trim();

        try
        {
            await SendTextHttp(targetChatId, greeting, CancellationToken.None);

            await mongoLogs.RegisterConnection(
                targetChatId,
                "startup",
                "online",
                "Hanna inició correctamente.",
                CancellationToken.None);

            Console.WriteLine($"[Telegram] Saludo inicial enviado solo a: {targetChatId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Telegram] No pude enviar saludo inicial: {ex.Message}");
        }
    }

    private long ResolveStartupGreetingChatId()
    {
        if (config.LocalChatId != 0)
            return config.LocalChatId;

        if (config.AllowedChats.Count > 0)
            return config.AllowedChats.First();

        return 0;
    }

    private async Task SendTextHttp(long chatId, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.TelegramToken))
            return;

        string url = $"https://api.telegram.org/bot{config.TelegramToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        try
        {
            await httpClient.PostAsync(url, content, cancellationToken);
        }
        catch
        {
        }
    }

    private async Task RegisterMessageSafe(long chatId, string author, string message, CancellationToken cancellationToken)
    {
        if (ShadowModeService.IsShadowActive(chatId))
            return;

        if (IsShadowCommand(message))
            return;

        await logs.RegisterMessage(chatId, author, message, cancellationToken);
    }

    private static void PrintUserConsole(long chatId, string text)
    {
        if (ShadowModeService.IsShadowActive(chatId))
            return;

        if (IsShadowCommand(text))
            return;

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine();
        Console.WriteLine($"👤 Usuario → {chatId}:");
        Console.ResetColor();

        Console.WriteLine(text);
    }

    private static bool IsShadowCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        string clean = text.Trim();

        clean = Regex.Replace(clean, @"^/\s+", "/");
        clean = Regex.Replace(clean, @"\s+", " ").Trim();

        return clean.StartsWith("/shadow", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMiIdCommand(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        text = text.Trim();
        text = Regex.Replace(text, @"^/\s+", "/");
        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text.Equals("/miid", StringComparison.OrdinalIgnoreCase);
    }

    private Task HandleErrorAsync(ITelegramBotClient client, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Telegram Error ({source}): {exception.Message}");
        return Task.CompletedTask;
    }
}

