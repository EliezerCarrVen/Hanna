using Hanna.Core;

namespace Hanna.Services;

internal sealed class TrustedWebSearchService
{
    private readonly AppConfig config;

    public TrustedWebSearchService(AppConfig config)
    {
        this.config = config;
    }

    public string BuildSearchUrl(string query)
    {
        string trustedPath = Path.Combine(config.BaseDirectory, "prompts_hanna", "trusted_sources.json");
        string sites = "site:gob.mx OR site:inegi.org.mx OR site:banxico.org.mx OR site:conagua.gob.mx OR site:cisa.gov OR site:learn.microsoft.com";

        try
        {
            if (File.Exists(trustedPath))
            {
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(trustedPath, Encoding.UTF8));
                var domains = new List<string>();

                foreach (var category in doc.RootElement.EnumerateObject())
                {
                    if (category.Value.ValueKind != JsonValueKind.Array)
                        continue;

                    foreach (var url in category.Value.EnumerateArray())
                    {
                        string? value = url.GetString();
                        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
                            domains.Add("site:" + uri.Host.Replace("www.", ""));
                    }
                }

                if (domains.Count > 0)
                    sites = string.Join(" OR ", domains.Take(12));
            }
        }
        catch
        {
        }

        return "https://www.google.com/search?q=" + Uri.EscapeDataString($"{query} {sites}");
    }
}
