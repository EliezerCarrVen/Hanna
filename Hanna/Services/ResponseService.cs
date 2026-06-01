using Hanna.Core;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Telegram.Bot;

namespace Hanna.Services;

internal sealed class ResponseService
{
    private readonly AppConfig config;
    private readonly FileStorageService storage;
    private readonly TtsService tts;
    private readonly HttpClient httpClient = new HttpClient();

    public ResponseService(AppConfig config, FileStorageService storage, TtsService tts)
    {
        this.config = config;
        this.storage = storage;
        this.tts = tts;
    }

    public string GetMode(long chatId) => storage.GetResponseMode(chatId);

    public Task SetMode(long chatId, string mode, CancellationToken cancellationToken)
        => storage.SetResponseMode(chatId, mode, cancellationToken);

    public async Task Send(TelegramBotClient botClient, long chatId, string text, CancellationToken cancellationToken, bool forceText = false)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        text = SecretSanitizer.Sanitize(NeutralizeRegionalisms(text));

        if (string.IsNullOrWhiteSpace(text))
            return;

        PrintHannaConsole(chatId, text);

        string mode = forceText ? "texto" : storage.GetResponseMode(chatId);
        bool containsCode = ContainsCodeBlock(text);
        bool sendTextBeforeTts = EnvBool("HANNA_SEND_TEXT_BEFORE_TTS", true);
        bool ttsBackground = EnvBool("HANNA_TTS_BACKGROUND", true);
        int ttsMaxChars = EnvInt("HANNA_TTS_MAX_CHARS", 650);
        bool textWasSent = false;

        if (mode == "texto" || mode == "ambos" || containsCode || (mode == "audio" && sendTextBeforeTts))
        {
            await SendLongText(chatId, text, cancellationToken);
            textWasSent = true;
        }

        if (mode == "audio" || mode == "ambos")
        {
            string voiceText = BuildVoiceText(text, containsCode);

            if (ttsMaxChars > 0 && voiceText.Length > ttsMaxChars)
                voiceText = voiceText[..ttsMaxChars].Trim() + "... Te dejé el resto por escrito.";

            if (!string.IsNullOrWhiteSpace(voiceText))
            {
                bool sendTextIfAudioFails = !textWasSent;

                if (ttsBackground)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await SendVoiceInParts(chatId, voiceText, CancellationToken.None, sendTextIfAudioFails);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[TTS background Error]: {ex.Message}");
                        }
                    });
                }
                else
                {
                    await SendVoiceInParts(chatId, voiceText, cancellationToken, sendTextIfAudioFails);
                }
            }
        }
    }

    public async Task SendLongText(long chatId, string text, CancellationToken cancellationToken)
    {
        const int limit = 3900;

        while (text.Length > limit)
        {
            int cut = text.LastIndexOf('\n', limit);

            if (cut < 100)
                cut = limit;

            string part = text[..cut].Trim();

            await SendTextHttp(chatId, part, cancellationToken);

            text = text[cut..].Trim();
        }

        if (!string.IsNullOrWhiteSpace(text))
            await SendTextHttp(chatId, text, cancellationToken);
    }

    private async Task SendVoiceInParts(long chatId, string text, CancellationToken cancellationToken, bool sendTextIfAudioFails)
    {
        foreach (var part in Utilities.TextTools.SplitForVoice(text, 420))
        {
            string? audio = await tts.Generate(part, cancellationToken);

            if (string.IsNullOrWhiteSpace(audio) || !File.Exists(audio))
            {
                if (sendTextIfAudioFails)
                    await SendTextHttp(chatId, part, cancellationToken);

                continue;
            }

            await using var stream = File.OpenRead(audio);
            await SendVoiceHttp(chatId, stream, "respuesta.mp3", cancellationToken);

            try
            {
                File.Delete(audio);
            }
            catch
            {
            }
        }
    }

    private string BuildVoiceText(string originalText, bool containsCode)
    {
        if (string.IsNullOrWhiteSpace(originalText))
            return "";

        string textForVoice = originalText;

        if (containsCode)
            textForVoice = ConvertCodeResponseToSpeech(originalText);

        textForVoice = Utilities.TextTools.CleanForVoice(textForVoice);
        textForVoice = HumanizeForSpeech(textForVoice);

        return NeutralizeRegionalisms(textForVoice).Trim();
    }

    private static string NeutralizeRegionalisms(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        int level = EnvInt("HANNA_MEXICANISMS_LEVEL", 0);
        if (level > 20)
            return text;

        var replacements = new (string Pattern, string Replacement)[]
        {
            (@"\b[¿?¡!]*\s*qué onda[¿?¡!]*", "¿Cómo estás?"),
            (@"\bque onda\b", "cómo estás"),
            (@"\bqué pedo\b", "qué sucede"),
            (@"\bque pedo\b", "qué sucede"),
            (@"\bno manches\b", "vaya"),
            (@"\bórale\b", "de acuerdo"),
            (@"\borale\b", "de acuerdo"),
            (@"\bándale\b", "de acuerdo"),
            (@"\bandale\b", "de acuerdo"),
            (@"\barre\b", "de acuerdo"),
            (@"\bsimón\b", "sí"),
            (@"\bsimon\b", "sí"),
            (@"\bnel\b", "no"),
            (@"\bchido\b", "bien"),
            (@"\bgacho\b", "desagradable"),
            (@"\bcompa\b", ""),
            (@"\bcarnal\b", ""),
            (@"\bwey\b", ""),
            (@"\bgüey\b", ""),
            (@"\bmexa\b", "mexicano"),
            (@"\bjale\b", "tarea"),
            (@"\bnos aventamos el trabajo\b", "lo hacemos"),
            (@"\bnos aventamos la tarea\b", "lo hacemos"),
            (@"\bnos aventamos el jale\b", "lo hacemos"),
            (@"\btraemos entre manos\b", "vamos a trabajar"),
            (@"\bqué tarea traemos entre manos\b", "en qué trabajamos"),
            (@"\bva\b", "de acuerdo"),
            (@"\bsale\b", "de acuerdo")
        };

        foreach (var (pattern, replacement) in replacements)
            text = Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        text = Regex.Replace(text, @"\s{2,}", " ");
        text = Regex.Replace(text, @"\s+([,.!?])", "$1");
        text = Regex.Replace(text, @"^[,.;:\s]+", "");

        return text.Trim();
    }

    private static bool ContainsCodeBlock(string text)
    {
        return Regex.IsMatch(text, @"```[\s\S]*?```");
    }

    private static string ConvertCodeResponseToSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        int codeBlocks = Regex.Matches(text, @"```[\s\S]*?```").Count;

        string withoutCode = Regex.Replace(text, @"```[\s\S]*?```", " [BLOQUE_CODIGO] ").Trim();

        withoutCode = Regex.Replace(withoutCode, @"\s+", " ").Trim();

        string codeMessage = codeBlocks switch
        {
            1 => "Te mandé el código completo por escrito. No lo voy a leer entero porque sería una tortura auditiva.",
            > 1 => $"Te mandé {codeBlocks} bloques de código por escrito. Los dejo en texto para que puedas copiarlos sin sufrir.",
            _ => ""
        };

        withoutCode = withoutCode.Replace("[BLOQUE_CODIGO]", codeMessage);

        withoutCode = Regex.Replace(withoutCode, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(withoutCode))
            return codeMessage;

        return withoutCode;
    }

    private static string HumanizeForSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = ConvertSpotifyLikedListToSpeech(text);
        text = ConvertCodeInstructionsToSpeech(text);
        text = ConvertNumberedListToSpeech(text);
        text = ConvertBulletListToSpeech(text);
        text = ConvertTechnicalSymbolsToSpeech(text);

        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }

    private static string ConvertSpotifyLikedListToSpeech(string text)
    {
        var lines = text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (lines.Count == 0)
            return text;

        bool isSpotifyLiked =
            lines[0].Contains("Me gusta de Spotify", StringComparison.OrdinalIgnoreCase) ||
            lines[0].Contains("Liked", StringComparison.OrdinalIgnoreCase);

        if (!isSpotifyLiked)
            return text;

        var songs = new List<(int Number, string Title, string Artist)>();

        foreach (string line in lines.Skip(1))
        {
            var match = Regex.Match(line, @"^(\d+)\.\s*(.+?)\s*-\s*(.+)$");

            if (!match.Success)
                continue;

            int number = int.Parse(match.Groups[1].Value);
            string title = match.Groups[2].Value.Trim();
            string artist = match.Groups[3].Value.Trim();

            songs.Add((number, title, artist));
        }

        if (songs.Count == 0)
            return text;

        var sb = new StringBuilder();

        sb.Append($"Te encontré tus primeros {NumberToSpanish(songs.Count)} Me gusta de Spotify. ");

        foreach (var song in songs.Take(10))
        {
            sb.Append($"El {OrdinalToSpanish(song.Number)} es {song.Title}, de {song.Artist}. ");
        }

        return sb.ToString().Trim();
    }

    private static string ConvertCodeInstructionsToSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        bool looksLikeCodeAnswer =
            text.Contains("código", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("codigo", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("Program.cs", StringComparison.OrdinalIgnoreCase) ||
            text.Contains(".cs", StringComparison.OrdinalIgnoreCase) ||
            text.Contains("archivo", StringComparison.OrdinalIgnoreCase);

        if (!looksLikeCodeAnswer)
            return text;

        text = Regex.Replace(
            text,
            @"Reemplaza\s+todo\s+tu\s+archivo\s*:?",
            "Reemplaza todo el archivo.",
            RegexOptions.IgnoreCase);

        text = Regex.Replace(
            text,
            @"Copia\s+y\s+pega\s+.*?:",
            "Copia y pega el código que te mandé por escrito.",
            RegexOptions.IgnoreCase);

        text = Regex.Replace(
            text,
            @"```[\s\S]*?```",
            "El código está en el mensaje de texto.",
            RegexOptions.IgnoreCase);

        return text;
    }

    private static string ConvertNumberedListToSpeech(string text)
    {
        var lines = text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        if (lines.Count < 2)
            return text;

        var numbered = lines
            .Where(x => Regex.IsMatch(x, @"^\d+\.\s+"))
            .ToList();

        if (numbered.Count < 2)
            return text;

        var sb = new StringBuilder();

        string intro = lines.FirstOrDefault(x => !Regex.IsMatch(x, @"^\d+\.\s+")) ?? "Estos son los resultados:";
        sb.Append(intro.TrimEnd(':', '.') + ". ");

        foreach (string line in numbered.Take(10))
        {
            var match = Regex.Match(line, @"^(\d+)\.\s*(.+)$");

            if (!match.Success)
                continue;

            int number = int.Parse(match.Groups[1].Value);
            string item = match.Groups[2].Value.Trim();

            item = item.Replace(" - ", ", de ");

            sb.Append($"El {OrdinalToSpanish(number)} es {item}. ");
        }

        return sb.ToString().Trim();
    }

    private static string ConvertBulletListToSpeech(string text)
    {
        var lines = text
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();

        var bullets = lines
            .Where(x => x.StartsWith("- ") || x.StartsWith("• "))
            .ToList();

        if (bullets.Count < 2)
            return text;

        var sb = new StringBuilder();

        string intro = lines.FirstOrDefault(x => !x.StartsWith("- ") && !x.StartsWith("• ")) ?? "Estos son los puntos:";
        sb.Append(intro.TrimEnd(':', '.') + ". ");

        int index = 1;

        foreach (string bullet in bullets.Take(10))
        {
            string item = bullet.TrimStart('-', '•', ' ').Trim();
            sb.Append($"Punto {NumberToSpanish(index)}: {item}. ");
            index++;
        }

        return sb.ToString().Trim();
    }

    private static string ConvertTechnicalSymbolsToSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = Regex.Replace(text, @"\.cs\b", " punto C sharp", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\.env\b", " punto env", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\.txt\b", " punto texto", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\.json\b", " punto json", RegexOptions.IgnoreCase);

        text = text.Replace("HannaEnv.env", "Hanna env");
        text = text.Replace("Program.cs", "Program punto C sharp");
        text = text.Replace("ResponseService.cs", "Response Service punto C sharp");
        text = text.Replace("TtsService.cs", "TTS Service punto C sharp");

        text = Regex.Replace(text, @"\bGroq\b", "Groc", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bGemini\b", "Gemini", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\bSpotify\b", "Spotify", RegexOptions.IgnoreCase);

        return text;
    }

    private static string OrdinalToSpanish(int number)
    {
        return number switch
        {
            1 => "primero",
            2 => "segundo",
            3 => "tercero",
            4 => "cuarto",
            5 => "quinto",
            6 => "sexto",
            7 => "séptimo",
            8 => "octavo",
            9 => "noveno",
            10 => "décimo",
            _ => $"número {number}"
        };
    }

    private static string NumberToSpanish(int number)
    {
        return number switch
        {
            1 => "uno",
            2 => "dos",
            3 => "tres",
            4 => "cuatro",
            5 => "cinco",
            6 => "seis",
            7 => "siete",
            8 => "ocho",
            9 => "nueve",
            10 => "diez",
            11 => "once",
            12 => "doce",
            13 => "trece",
            14 => "catorce",
            15 => "quince",
            16 => "dieciséis",
            17 => "diecisiete",
            18 => "dieciocho",
            19 => "diecinueve",
            20 => "veinte",
            _ => number.ToString()
        };
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

    private async Task SendTextHttp(long chatId, string text, CancellationToken cancellationToken)
    {
        text = SecretSanitizer.Sanitize(text);
        if (string.IsNullOrWhiteSpace(text))
            return;

        if (string.IsNullOrWhiteSpace(config.TelegramToken))
            return;

        string url = $"https://api.telegram.org/bot{config.TelegramToken}/sendMessage";

        var payload = new
        {
            chat_id = chatId,
            text = text
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[Telegram sendMessage Error]: {error}");
        }
    }

    private async Task SendVoiceHttp(long chatId, Stream audioStream, string fileName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.TelegramToken))
            return;

        string url = $"https://api.telegram.org/bot{config.TelegramToken}/sendVoice";

        using var content = new MultipartFormDataContent();

        audioStream.Position = 0;

        var streamContent = new StreamContent(audioStream);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");

        content.Add(new StringContent(chatId.ToString()), "chat_id");
        content.Add(streamContent, "voice", fileName);

        using var response = await httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            Console.WriteLine($"[Telegram sendVoice Error]: {error}");
        }
    }
    private static void PrintHannaConsole(long chatId, string text)
    {
        if (ShadowModeService.IsShadowActive(chatId))
            return;

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine();
        Console.WriteLine($"🤖 Hanna → {chatId}:");
        Console.ResetColor();

        Console.WriteLine(text);
    }
}