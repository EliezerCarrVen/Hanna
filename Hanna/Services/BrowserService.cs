namespace Hanna.Services;

internal sealed class BrowserService
{
    public string OpenUrlOrSearch(string text, bool search)
    {
        string query = ExtractQuery(text);

        if (string.IsNullOrWhiteSpace(query))
            return "Dime qué página o búsqueda quieres abrir.";

        string url;

        if (!search && Uri.TryCreate(query, UriKind.Absolute, out _))
            url = query;
        else if (!search && query.Contains("."))
            url = "https://" + query;
        else
            url = "https://www.google.com/search?q=" + Uri.EscapeDataString(query);

        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            return search ? $"Buscando: {query}" : $"Abriendo: {query}";
        }
        catch (Exception ex)
        {
            return $"No pude abrir el navegador. Detalle: {ex.Message}";
        }
    }

    private static string ExtractQuery(string text)
    {
        string clean = text;
        string[] remove = { "hanna", "abre", "abrir", "entra a", "navega a", "busca en internet", "busca en google", "investiga en web", "busqueda web", "búsqueda web", "pagina", "página", "web", "url", "sitio" };

        foreach (string phrase in remove)
            clean = Regex.Replace(clean, Regex.Escape(phrase), " ", RegexOptions.IgnoreCase);

        clean = Regex.Replace(clean, @"\s+", " ").Trim();
        return clean;
    }
}
