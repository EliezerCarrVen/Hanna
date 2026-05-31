using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class TrustedWebSearchSkill : ISkill
{
    private readonly TrustedWebSearchService web;
    private readonly BrowserService browser;

    public TrustedWebSearchSkill(TrustedWebSearchService web, BrowserService browser)
    {
        this.web = web;
        this.browser = browser;
    }

    public bool CanHandle(IntentResult intent) => intent.Type == IntentType.TrustedWebSearch;

    public Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string url = web.BuildSearchUrl(originalText);
        OpenUrlInBrowser(url); // Llama al método personalizado
        return Task.FromResult(SkillResult.Text("Abrí una búsqueda con fuentes confiables para esa información. Para responder dentro de Hanna con resumen automático falta conectar un motor de lectura web real o un buscador/API."));
    }

    private static void OpenUrlInBrowser(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            // Abre la URL en el navegador predeterminado
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
        }
    }
}
