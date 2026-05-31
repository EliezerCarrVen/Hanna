using Hanna.Core;
using Hanna.Models;
using Hanna.Services;
using Hanna.Spotify;
using Telegram.Bot;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Hanna.Skills;

internal sealed class SystemSkill : ISkill
{
    private readonly AppConfig config;
    private readonly FileStorageService storage;
    private readonly SpotifyAuthService spotifyAuth;
    private readonly SpotifyPlaybackService playback;
    private readonly ResponseService response;
    private readonly WebVideoDownloadService webVideo;
    private readonly ShadowModeService shadow;
    private readonly HornyDownloaderService hd;
    private readonly HttpClient httpClient = new();

    public SystemSkill(
        AppConfig config,
        FileStorageService storage,
        SpotifyAuthService spotifyAuth,
        SpotifyPlaybackService playback,
        ResponseService response,
        WebVideoDownloadService webVideo,
        ShadowModeService shadow,
        HornyDownloaderService hd)
    {
        this.config = config;
        this.storage = storage;
        this.spotifyAuth = spotifyAuth;
        this.playback = playback;
        this.response = response;
        this.webVideo = webVideo;
        this.shadow = shadow;
        this.hd = hd;
    }

    public bool CanHandle(IntentResult intent)
        => intent.Type == IntentType.SystemCommand || intent.Type == IntentType.SpotifyDevices;

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string text = NormalizeCommand(originalText);

        if (text.Equals("/h", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("/help", StringComparison.OrdinalIgnoreCase) ||
            text.Equals("/ayuda", StringComparison.OrdinalIgnoreCase) ||
            text.StartsWith("/skills", StringComparison.OrdinalIgnoreCase))
        {
            return SkillResult.Text(BuildHelpText(), true);
        }

        if (text.Equals("/miid", StringComparison.OrdinalIgnoreCase))
            return SkillResult.Text($"Tu chatId es: {chatId}", true);

        if (text.StartsWith("/shadow", StringComparison.OrdinalIgnoreCase))
        {
            string msg = shadow.Activate(chatId);
            return SkillResult.Text(msg, true);
        }

        if (text.StartsWith("/d ", StringComparison.OrdinalIgnoreCase) || text.Equals("/d", StringComparison.OrdinalIgnoreCase))
            return await DownloadByCommand(chatId, text, cancellationToken);

        if (text.StartsWith("/hd ", StringComparison.OrdinalIgnoreCase) && ContainsUrl(text))
            return await DownloadByCommand(chatId, ConvertHdToDownloadCommand(text), cancellationToken);

        if (text.Equals("/hd", StringComparison.OrdinalIgnoreCase) || text.Equals("/hd_help", StringComparison.OrdinalIgnoreCase))
            return SkillResult.Text(hd.Help(), true);

        if (text.StartsWith("/hd_add", StringComparison.OrdinalIgnoreCase) || text.StartsWith("/hd_dl", StringComparison.OrdinalIgnoreCase))
            return SkillResult.Text(hd.DownloadDisabledMessage(), true);

        if (text.StartsWith("/hd_status", StringComparison.OrdinalIgnoreCase) || text.StartsWith("/hd_queue", StringComparison.OrdinalIgnoreCase))
            return SkillResult.Text(hd.Status(), true);

        if (text.StartsWith("/hd_downloads", StringComparison.OrdinalIgnoreCase))
            return SkillResult.Text(hd.ListDownloads(), true);

        if (text.StartsWith("/hd_logs", StringComparison.OrdinalIgnoreCase))
            return SkillResult.Text(hd.Logs(), true);

        if (text.StartsWith("/hd_send", StringComparison.OrdinalIgnoreCase))
            return await HdSend(chatId, text, cancellationToken);

        if (text.StartsWith("/modo", StringComparison.OrdinalIgnoreCase))
        {
            string normalized = Utilities.TextTools.Normalize(text);
            string mode = normalized.Contains("texto") ? "texto" :
                          normalized.Contains("audio") ? "audio" :
                          normalized.Contains("ambos") ? "ambos" : "";

            if (string.IsNullOrWhiteSpace(mode))
                return SkillResult.Text("Elige un modo: /modo texto, /modo audio o /modo ambos", true);

            await response.SetMode(chatId, mode, cancellationToken);
            return SkillResult.Text($"Modo cambiado a: {mode}", true);
        }

        if (text.StartsWith("/spotify_reset", StringComparison.OrdinalIgnoreCase))
        {
            spotifyAuth.Reset(chatId);
            return SkillResult.Text("Borré el token local de Spotify. Ahora escribe /auth para vincular otra vez.", true);
        }

        if (text.StartsWith("/spotify_status", StringComparison.OrdinalIgnoreCase))
        {
            bool has = spotifyAuth.HasToken(chatId);
            string? access = await spotifyAuth.GetUserAccessToken(chatId, cancellationToken);

            return SkillResult.Text(has
                ? access != null
                    ? "Spotify está vinculado y el token se puede renovar."
                    : "Hay token guardado, pero no pude renovarlo. Usa /spotify_reset y luego /auth."
                : "Spotify no está vinculado. Usa /auth.", true);
        }

        if (text.StartsWith("/auth", StringComparison.OrdinalIgnoreCase))
        {
            string code = SpotifyAuthService.ExtractCode(text);

            if (string.IsNullOrWhiteSpace(code))
            {
                string url = spotifyAuth.BuildAuthUrl(chatId);

                return SkillResult.Text(
                    "Vincula Spotify:\n\n" +
                    "1. Abre este enlace:\n" + url + "\n\n" +
                    "2. Inicia sesión y acepta permisos.\n" +
                    "3. Copia el enlace completo que queda en la barra de dirección.\n" +
                    "4. Pégalo aquí así:\n/auth ENLACE_COMPLETO\n\n" +
                    "Cada código sirve una sola vez.", true);
            }

            bool ok = await spotifyAuth.ExchangeCode(chatId, code, cancellationToken);

            return SkillResult.Text(ok
                ? "Tu cuenta de Spotify quedó vinculada correctamente con permisos de lectura, Me gusta, reproducción y playlists."
                : "El código no funcionó. Pide otro con /auth y pega el enlace completo inmediatamente después de autorizar.", true);
        }

        if (text.StartsWith("/dispositivos", StringComparison.OrdinalIgnoreCase) || intent.Type == IntentType.SpotifyDevices)
            return SkillResult.Text(await BuildDevicesList(chatId, cancellationToken), true);

        if (text.StartsWith("/dispositivo", StringComparison.OrdinalIgnoreCase))
        {
            int index = Utilities.TextTools.ExtractNumber(text);

            if (index <= 0)
                return SkillResult.Text("Usa /dispositivo 1, /dispositivo 2, etc.", true);

            bool ok = await playback.SetPreferredDeviceByIndex(chatId, index, cancellationToken);

            return SkillResult.Text(ok
                ? $"Listo. Dejé el dispositivo {index} como preferido."
                : "No pude seleccionar ese dispositivo. Usa /dispositivos para ver la lista.", true);
        }

        if (text.StartsWith("/", StringComparison.OrdinalIgnoreCase))
            return SkillResult.Text("No reconozco ese comando. Usa /h para ver la lista disponible.", true);

        return SkillResult.NotHandled();
    }

    private async Task<SkillResult> HdSend(long chatId, string text, CancellationToken cancellationToken)
    {
        int index = Utilities.TextTools.ExtractNumber(text);

        if (index <= 0)
            return SkillResult.Text("Usa /hd_send 1, /hd_send 2, etc. Primero puedes revisar con /hd_downloads.", true);

        string? file = hd.GetDownloadByIndex(index, out string error);

        if (string.IsNullOrWhiteSpace(file))
            return SkillResult.Text(error, true);

        bool sent = await SendVideo(chatId, file, cancellationToken);

        return sent
            ? SkillResult.Silent()
            : SkillResult.Text("No pude enviar ese video por Telegram.", true);
    }

    private async Task<SkillResult> DownloadByCommand(long chatId, string text, CancellationToken cancellationToken)
    {
        await SendText(chatId, "Descargando video con /d. Primero pruebo pull-vids y luego yt-dlp si hace falta.", cancellationToken);

        var result = await webVideo.DownloadVideo(text, chatId, cancellationToken);

        if (!result.Success)
            return SkillResult.Text(result.Message, true);

        bool sent = await SendVideo(chatId, result.FilePath, cancellationToken);

        if (!sent)
            return SkillResult.Text("Descargué el video, pero no pude enviarlo por Telegram. Archivo local: " + result.FilePath, true);

        try
        {
            File.Delete(result.FilePath);
        }
        catch
        {
        }

        return SkillResult.Silent();
    }

    private async Task SendText(long chatId, string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.TelegramToken))
            return;

        string url = $"https://api.telegram.org/bot{config.TelegramToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text
        };

        using var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        await httpClient.PostAsync(url, content, cancellationToken);
    }

    private async Task<bool> SendVideo(long chatId, string filePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.TelegramToken))
            return false;

        if (!File.Exists(filePath))
            return false;

        string url = $"https://api.telegram.org/bot{config.TelegramToken}/sendVideo";

        await using var stream = File.OpenRead(filePath);

        using var content = new MultipartFormDataContent();
        var streamContent = new StreamContent(stream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("video/mp4");

        content.Add(new StringContent(chatId.ToString()), "chat_id");
        content.Add(streamContent, "video", Path.GetFileName(filePath));

        using var telegramResponse = await httpClient.PostAsync(url, content, cancellationToken);

        if (!telegramResponse.IsSuccessStatusCode)
        {
            string error = await telegramResponse.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine("[Telegram sendVideo Error] " + error);
        }

        return telegramResponse.IsSuccessStatusCode;
    }

    private async Task<string> BuildDevicesList(long chatId, CancellationToken cancellationToken)
    {
        var devices = await playback.GetDevices(chatId, cancellationToken);

        if (devices.Count == 0)
            return "No encontré dispositivos disponibles de Spotify. Abre Spotify en tu PC o celular y reproduce cualquier canción unos segundos.";

        var sb = new StringBuilder();
        sb.AppendLine("Dispositivos Spotify disponibles:");

        for (int i = 0; i < devices.Count; i++)
        {
            var d = devices[i];
            sb.AppendLine($"{i + 1}. {d.Name} ({d.Type}) {(d.IsActive ? "- ACTIVO" : "")}");
        }

        sb.AppendLine();
        sb.AppendLine("Para dejar uno como preferido usa /dispositivo número.");

        return sb.ToString().Trim();
    }

    private static string NormalizeCommand(string originalText)
    {
        string text = (originalText ?? "").Trim();
        text = Regex.Replace(text, @"^/\s+", "/");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }

    private string BuildHelpText()
    {
        return
            "🤖 Comandos de Hanna:\n\n" +

            "📌 General:\n" +
            "/h - muestra esta ayuda.\n" +
            "/miid - muestra tu chatId.\n" +
            "/modo texto - responde solo por texto.\n" +
            "/modo audio - responde solo por audio.\n" +
            "/modo ambos - manda texto y audio.\n" +
            "/shadow - activa modo privado por 10 minutos.\n\n" +

            "🧠 Motor IA:\n" +
            "Hanna usa Ollama / modo local - usa el LLM local.\n" +
            "Panel web: http://127.0.0.1:8787 - administra Hanna sin abrir carpetas.\n" +
            "modo original - Groq principal y Gemini como respaldo.\n" +
            "usa Groq - usa Groq como motor.\n" +
            "usa Gemini - usa Gemini como motor.\n" +
            "modo híbrido - Groq genera, Gemini revisa y Groq responde final.\n" +
            "Hanna usa OpenRouter / modo OpenRouter - usa el switch inteligente de modelos vía OpenRouter.\n" +
            "/senior - activa ARCHITECT: arquitectura, seguridad y refactorización compleja.\n" +
            "/dev - activa ENGINEER: programación diaria y debugging.\n" +
            "/ops - activa OPERATOR: archivos, logs, servidor y bajo consumo.\n" +
            "/analyst - activa ANALYST: reportes, resúmenes y datos administrativos.\n" +
            "/personas - lista personalidades disponibles.\n" +
            "/persona actual - muestra personalidad activa.\n" +
            "/tokens - reporte de tokens del día.\n" +
            "/tokens archivo \"RUTA\" - estima tokens/costo antes de subir o mandar un archivo al modelo.\n" +
            "En computadora Hanna prioriza Ollama local; en Telegram puede usar OpenRouter si lo activas.\n\n" +

            "🖥️ Computadora / agente local:\n" +
            "F8 - escuchar por micrófono sin ventana flotante.\n" +
            "AltGr+Enter - escuchar por micrófono con ventana flotante.\n" +
            "AltGr+Shift+H - alternativa con ventana flotante.\n" +
            "F9 - analiza pantalla y genera un .txt con código si detecta una consigna.\n" +
            "Hanna necesito este código... - genera código con contexto de tus proyectos.\n" +
            "enciende cámara / apaga cámara / estado cámara.\n" +
            "activa indicador de cámara / desactiva indicador de cámara.\n\n" +

            "📁 Proyectos:\n" +
            "Coloca tus proyectos en HANNA_PROJECTS_DIRECTORY.\n" +
            "Hanna tomará fragmentos de código de ahí para responder mejor.\n" +
            "Las salidas generadas se guardan en HANNA_AGENT_OUTPUT_DIRECTORY.\n\n" +

            "🎵 Spotify:\n" +
            "/auth - vincula Spotify.\n" +
            "/spotify_reset - borra token local de Spotify.\n" +
            "/spotify_status - revisa conexión de Spotify.\n" +
            "/dispositivos - lista dispositivos de Spotify.\n" +
            "/dispositivo 1 - deja ese dispositivo como preferido.\n" +
            "Reproduce CANCIÓN - reproduce en Spotify por defecto.\n" +
            "Agrega CANCIÓN a la fila - agrega a la cola.\n" +
            "Qué hay en la fila - muestra la cola actual.\n" +
            "Crea una playlist llamada NOMBRE - crea playlist.\n" +
            "Agrega CANCIÓN a la playlist NOMBRE - agrega canción.\n\n" +

            "🎬 Video:\n" +
            "/d LINK - descarga video compatible y lo manda por Telegram.\n" +
            "/hd - ayuda HD.\n" +
            "/hd_status - estado de carpeta/logs HD.\n" +
            "/hd_downloads - lista videos ya descargados por la app externa.\n" +
            "/hd_send 1 - manda por Telegram el video número 1.\n" +
            "/hd_logs - muestra logs recientes.\n\n" +

            "⚙️ Rutinas:\n" +
            "modo estudio - cambia a texto.\n" +
            "modo música - activa música y modo ambos.\n" +
            "buenas noches - pausa Spotify y cambia a texto.\n\n" +

            "🌐 Otros:\n" +
            "Clima en CIUDAD - consulta clima.\n" +
            "Analiza una imagen - manda imagen y Hanna la describe.\n" +
            "Busca en Google TEXTO - abre búsqueda web.\n" +
            "Lista archivos / lee archivo / busca archivo - control básico de archivos.\n" +
            "Panel web local - cambia voz, motores, directorios, overlay, personalidad, skills, tareas, archivos y móvil sin reiniciar.";
    }
    private static bool ContainsUrl(string text)
    {
        return Regex.IsMatch(text, @"https?://\S+", RegexOptions.IgnoreCase);
    }

    private static string ConvertHdToDownloadCommand(string text)
    {
        var match = Regex.Match(text, @"https?://\S+", RegexOptions.IgnoreCase);

        if (!match.Success)
            return "/d";

        string url = match.Value.Trim().TrimEnd('.', ',', ';', ')', ']');

        return "/d " + url;
    }
}