using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class DesktopSkill : ISkill
{
    private readonly AppLauncherService appLauncher;
    private readonly BrowserService browser;

    public DesktopSkill(AppLauncherService appLauncher, BrowserService browser)
    {
        this.appLauncher = appLauncher;
        this.browser = browser;
    }

    public bool CanHandle(IntentResult intent)
    {
        return intent.Type is IntentType.OpenApp or IntentType.OpenUrl or IntentType.BrowserSearch or IntentType.ComputerSettingsInfo;
    }

    public Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (intent.Type == IntentType.OpenApp)
            return Task.FromResult(SkillResult.Text(appLauncher.Open(originalText), true));

        if (intent.Type == IntentType.OpenUrl)
            return Task.FromResult(SkillResult.Text(browser.OpenUrlOrSearch(originalText, false), true));

        if (intent.Type == IntentType.BrowserSearch)
            return Task.FromResult(SkillResult.Text(browser.OpenUrlOrSearch(originalText, true), true));

        return Task.FromResult(SkillResult.Text("La skill de configuración del equipo está preparada, pero todavía no haré cambios directos de sistema sin confirmación explícita.", true));
    }
}
