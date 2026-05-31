using Hanna.Core;

namespace Hanna.Services;

internal sealed class MediaControlService
{
    private readonly AppConfig config;
    private readonly BrowserService browser;
    private readonly HttpClient http = new();

    public MediaControlService(AppConfig config, BrowserService browser)
    {
        this.config = config;
        this.browser = browser;
    }

    public async Task<string> HandleNetflixPc(string text, CancellationToken cancellationToken)
    {
        string query = ExtractMediaQuery(text, "netflix");
        string url = string.IsNullOrWhiteSpace(query)
            ? "https://www.netflix.com/"
            : "https://www.netflix.com/search?q=" + Uri.EscapeDataString(query);

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            if (string.IsNullOrWhiteSpace(query))
                return "Abrí Netflix en la PC. Si tu sesión está iniciada, selecciona el perfil y contenido.";
            return $"Abrí Netflix en la PC y busqué: {query}. La reproducción automática puede requerir perfil/sesión por DRM.";
        }
        catch (Exception ex)
        {
            return "No pude abrir Netflix en la PC. Detalle: " + ex.Message;
        }
    }

    public async Task<string> HandleNetflixTvLg(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.LgTvBridgeUrl))
        {
            return "TV LG aún no está emparejada. Configura HANNA_LGTV_BRIDGE_URL con un puente local webOS/Home Assistant/lgtv-http-server. Puedo abrir Netflix en PC mientras tanto.";
        }

        string query = ExtractMediaQuery(text, "netflix");
        try
        {
            string baseUrl = config.LgTvBridgeUrl.TrimEnd('/');
            string url = baseUrl + "/launch?app=" + Uri.EscapeDataString(config.LgTvNetflixAppId);
            if (!string.IsNullOrWhiteSpace(query))
                url += "&query=" + Uri.EscapeDataString(query);

            using var resp = await http.GetAsync(url, cancellationToken);
            string body = await resp.Content.ReadAsStringAsync(cancellationToken);
            if (!resp.IsSuccessStatusCode)
                return $"Intenté abrir Netflix en TV LG, pero el puente respondió HTTP {(int)resp.StatusCode}: {body}";

            return string.IsNullOrWhiteSpace(query)
                ? "Envié la orden para abrir Netflix en la TV LG."
                : $"Envié la orden para abrir Netflix en la TV LG y buscar: {query}.";
        }
        catch (Exception ex)
        {
            return "No pude controlar la TV LG. Revisa que el puente webOS esté encendido. Detalle: " + ex.Message;
        }
    }

    public async Task<string> HandleYoutubeTvLg(string text, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.LgTvBridgeUrl))
            return "TV LG aún no está emparejada. Configura HANNA_LGTV_BRIDGE_URL para controlar YouTube en la TV.";

        string query = ExtractMediaQuery(text, "youtube");
        try
        {
            string url = config.LgTvBridgeUrl.TrimEnd('/') + "/launch?app=youtube";
            if (!string.IsNullOrWhiteSpace(query))
                url += "&query=" + Uri.EscapeDataString(query);
            using var resp = await http.GetAsync(url, cancellationToken);
            string body = await resp.Content.ReadAsStringAsync(cancellationToken);
            return resp.IsSuccessStatusCode ? "Envié la orden a YouTube en TV LG." : $"El puente TV LG respondió HTTP {(int)resp.StatusCode}: {body}";
        }
        catch (Exception ex)
        {
            return "No pude controlar YouTube en la TV LG. Detalle: " + ex.Message;
        }
    }

    private static string ExtractMediaQuery(string text, string platform)
    {
        string clean = text ?? "";
        string[] remove =
        {
            "hanna", "abre", "abrir", "busca", "buscar", "reproduce", "reproducir", "pon", "play",
            "en la pc", "en pc", "en la computadora", "en computadora", "en la tv lg", "en tv lg", "tv lg", "tele lg",
            platform
        };

        foreach (string phrase in remove)
            clean = Regex.Replace(clean, Regex.Escape(phrase), " ", RegexOptions.IgnoreCase);

        clean = Regex.Replace(clean, @"\b(serie|pelicula|película|plataforma|la|el|de|por favor)\b", " ", RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"\s+", " ").Trim(' ', ':', '-', '.', ',');
        return clean;
    }
}
