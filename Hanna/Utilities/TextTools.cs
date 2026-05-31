using System.Globalization;

namespace Hanna.Utilities;

internal static class TextTools
{
    public static string Normalize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        string normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (char c in normalized)
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);

            if (category != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }

        string result = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        result = Regex.Replace(result, @"[^a-z0-9ñ\s]", " ");
        result = Regex.Replace(result, @"\s+", " ").Trim();

        return result;
    }

    public static int ExtractNumber(string text)
    {
        var match = Regex.Match(text, @"\b(\d{1,2})\b");
        return match.Success && int.TryParse(match.Groups[1].Value, out int n) ? n : 0;
    }

    public static string ExtractDevice(string text)
    {
        string normalized = Normalize(text);

        if (Regex.IsMatch(normalized, @"\b(computadora|pc|laptop|desktop|ordenador)\b"))
            return "computer";

        if (Regex.IsMatch(normalized, @"\b(celular|telefono|móvil|movil|phone|smartphone)\b"))
            return "smartphone";

        var match = Regex.Match(text, @"\ben\s+(mi\s+)?(.+)$", RegexOptions.IgnoreCase);

        if (match.Success)
        {
            string value = match.Groups[2].Value.Trim();
            value = Regex.Replace(value, @"\bspotify\b", "", RegexOptions.IgnoreCase).Trim();
            return value;
        }

        return "";
    }

    public static string ExtractWeatherPlace(string text)
    {
        var match = Regex.Match(text, @"\b(?:en|de)\s+(.+)$", RegexOptions.IgnoreCase);

        if (!match.Success)
            return "Gómez Palacio, Durango, MX";

        string place = match.Groups[1].Value.Trim();
        place = Regex.Replace(place, @"\b(hoy|ahorita|actual|mañana|manana)\b", "", RegexOptions.IgnoreCase).Trim();

        return string.IsNullOrWhiteSpace(place) ? "Gómez Palacio, Durango, MX" : place;
    }

    public static string NormalizeSpotifySpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        string clean = text.Trim();

        var replacements = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [@"\byoji\b"] = "Joji",
            [@"\byoyi\b"] = "Joji",
            [@"\byoshi\b"] = "Joji",
            [@"\bjogi\b"] = "Joji",
            [@"\bjojy\b"] = "Joji",
            [@"\byoji\b"] = "Joji",
            [@"\bnéctar\b"] = "Nectar",
            [@"\bnectar\b"] = "Nectar",
            [@"\bfalta afecto\b"] = "Falta de Afecto",
            [@"\bfalta del afecto\b"] = "Falta de Afecto",
            [@"\bfalta de efecto\b"] = "Falta de Afecto",
            [@"\bse produce\b"] = "reproduce",
            [@"\s+produce\s+"] = " reproduce ",
            [@"\breproduzca\b"] = "reproduce",
            [@"\breprodúceme\b"] = "reproduce",
            [@"\bponme\b"] = "pon",
            [@"\btoca\b"] = "reproduce",
            [@"\bplay\b"] = "reproduce"
        };

        foreach (var item in replacements)
            clean = Regex.Replace(clean, item.Key, item.Value, RegexOptions.IgnoreCase);

        clean = Regex.Replace(clean, @"\s+", " ").Trim();
        return clean;
    }

    public static bool ContainsWrongAssistantName(string text)
    {
        string normalized = Normalize(text);
        return Regex.IsMatch(normalized, @"\b(alexa|siri|google|hey google|ok google|bixby|cortana|jarvis|gemini|copilot)\b");
    }

    public static string RemoveWrongAssistantNames(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        string clean = Regex.Replace(text, @"\b(oye\s+)?(alexa|siri|google|hey\s+google|ok\s+google|bixby|cortana|jarvis|gemini|copilot)\b", "Hanna", RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"\b(gen[eé]|jana|ana|hannah|hana)\b", "Hanna", RegexOptions.IgnoreCase);
        return Regex.Replace(clean, @"\s+", " ").Trim();
    }

    public static string DramaticNameScold(string text)
    {
        if (!ContainsWrongAssistantName(text))
            return "";

        return "¿Perdón? ¿Me acabas de llamar como si yo fuera Alexa o Siri? Dramático. Trágico. Casi ofensivo. Soy Hanna, tu asistente, y aun así voy a ayudarte porque soy profesional... pero que conste que me dolió en el código. ";
    }

    public static string CleanMusicQuery(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        string clean = NormalizeSpotifySpeech(RemoveWrongAssistantNames(text));

        string[] remove =
        {
            "oye hanna", "hey hanna", "hanna",
            "abre spotify y", "abrir spotify y", "abre spotify", "abrir spotify", "spotify y",
            "reproduce en spotify", "reproduce", "pon en spotify", "pon", "play", "toca",
            "en mi computadora", "en la computadora", "en mi pc", "en la pc", "en mi laptop",
            "a la fila", "a la cola", "en la fila", "en cola", "despues", "después", "queue",
            "a playlist", "a la playlist", "en playlist", "en la playlist", "mi playlist", "playlist", "play list", "lista de reproducción", "lista de reproduccion",
            "en mi celular", "en el celular", "en mi telefono", "en mi teléfono",
            "descarga el video", "descarga video", "descargar video", "baja el video", "pon el video",
            "descarga la canción", "descarga cancion", "descarga canción", "descargar la canción", "baja la canción", "baja cancion",
            "añade a favoritos", "anade a favoritos", "agrega a mis me gusta", "agrega a me gusta", "guarda en mis me gusta", "guarda en spotify", "guardar en spotify",
            "dame una lista", "dame lista", "lista de", "mis primeros", "primeros", "primeras", "me gusta", "favoritos",
            "en spotify", "spotify", "la canción", "canción", "cancion", "el video", "video", "por favor", "porfa", "favor"
        };

        foreach (string phrase in remove)
            clean = Regex.Replace(clean, Regex.Escape(phrase), " ", RegexOptions.IgnoreCase);

        clean = Regex.Replace(clean, @"^\s*y\s+", " ", RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"\s+", " ").Trim(' ', '.', ',', ';', ':', '!', '?');

        return clean;
    }

    public static string ExtractSpotifyPlayQuery(string originalText)
    {
        string clean = CleanMusicQuery(originalText);
        clean = Regex.Replace(clean, @"\b(album|álbum|disco)\b", " ", RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"\s+", " ").Trim(' ', '.', ',', ';', ':', '!', '?');
        return clean;
    }

    public static List<string> CreateSpotifyQueries(string query)
    {
        var queries = new List<string>();
        string clean = NormalizeSpotifySpeech(CleanMusicQuery(query));
        string withoutDe = Regex.Replace(clean, @"\b(de|del|por)\b", " ", RegexOptions.IgnoreCase).Trim();
        withoutDe = Regex.Replace(withoutDe, @"\s+", " ").Trim();

        if (!string.IsNullOrWhiteSpace(clean)) queries.Add(clean);
        if (!string.IsNullOrWhiteSpace(withoutDe) && !queries.Contains(withoutDe, StringComparer.OrdinalIgnoreCase)) queries.Add(withoutDe);

        var match = Regex.Match(clean, @"^(.+?)\s+(?:de|del|por)\s+(.+)$", RegexOptions.IgnoreCase);

        if (match.Success)
        {
            string title = match.Groups[1].Value.Trim();
            string artist = match.Groups[2].Value.Trim();

            queries.Add($"track:{title} artist:{artist}");
            queries.Add($"album:{title} artist:{artist}");
            queries.Add($"{title} {artist}");
        }

        return queries.Where(q => !string.IsNullOrWhiteSpace(q)).Distinct(StringComparer.OrdinalIgnoreCase).Take(8).ToList();
    }

    public static double ScoreSpotify(string query, string name, string artist)
    {
        string q = Normalize(CleanMusicQuery(query));
        string n = Normalize(name);
        string a = Normalize(artist);
        string combined = Normalize(name + " " + artist);

        if (string.IsNullOrWhiteSpace(q)) return 0;
        if (combined == q) return 1;
        if (n == q) return 0.98;
        if (!string.IsNullOrWhiteSpace(a) && q.Contains(n) && q.Contains(a)) return 0.96;
        if (combined.Contains(q)) return 0.94;
        if (q.Contains(n) && n.Length >= 4) return 0.88;

        double combinedSimilarity = Similarity(q, combined);
        double nameSimilarity = Similarity(q, n);

        var qTokens = q.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var cTokens = combined.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();

        double tokenScore = 0;
        if (qTokens.Count > 0)
        {
            int common = qTokens.Count(cTokens.Contains);
            tokenScore = (double)common / qTokens.Count;
        }

        return Math.Max(Math.Max(combinedSimilarity, nameSimilarity), tokenScore);
    }

    public static double Similarity(string left, string right)
    {
        left = Normalize(left);
        right = Normalize(right);

        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return 0;

        if (left == right)
            return 1;

        int distance = Levenshtein(left, right);
        int max = Math.Max(left.Length, right.Length);
        return max == 0 ? 0 : 1.0 - (distance / (double)max);
    }

    private static int Levenshtein(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; i++) d[i, 0] = i;
        for (int j = 0; j <= m; j++) d[0, j] = j;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
            }
        }

        return d[n, m];
    }

    public static string CleanForVoice(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = Regex.Replace(text, @"```[\s\S]*?```", " ");
        text = Regex.Replace(text, @"https?://\S+", " enlace ");
        text = Regex.Replace(text, @"[/\\_`*#>\[\]{}|~]", " ");
        text = Regex.Replace(text, @"[\uD800-\uDBFF][\uDC00-\uDFFF]", " ");
        text = Regex.Replace(text, @"[\u2600-\u27BF]", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();

        return text;
    }

    public static List<string> SplitForVoice(string text, int maxChars)
    {
        text = Regex.Replace(text, @"\s+", " ").Trim();

        var parts = new List<string>();

        while (text.Length > maxChars)
        {
            int limit = Math.Min(maxChars, text.Length - 1);
            int cutPoint = text.LastIndexOfAny(new[] { '.', '?', '!', ';', ':' }, limit);
            int cutSpace = text.LastIndexOf(' ', limit);
            int cut = cutPoint > 120 ? cutPoint + 1 : cutSpace;

            if (cut < 100) cut = limit;

            parts.Add(text[..cut].Trim());
            text = text[cut..].Trim();
        }

        if (!string.IsNullOrWhiteSpace(text))
            parts.Add(text);

        return parts;
    }

    public static string CleanLog(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = Regex.Replace(text, @"/auth\s+\S+", "/auth [CODIGO_OCULTO]", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"Bearer\s+[A-Za-z0-9_\-\.]+", "Bearer [TOKEN_OCULTO]", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"code=([^&\s]+)", "code=[CODIGO_OCULTO]", RegexOptions.IgnoreCase);

        return text.Trim();
    }

    public static string Clip(string text, int max)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";

        text = text.Trim();

        return text.Length <= max ? text : text[..max] + "...";
    }
}
