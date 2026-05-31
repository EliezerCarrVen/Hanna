using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class DynamicSkill : ISkill
{
    private readonly DynamicSkillService dynamicSkills;

    public DynamicSkill(DynamicSkillService dynamicSkills)
    {
        this.dynamicSkills = dynamicSkills;
    }

    public bool CanHandle(IntentResult intent)
    {
        return intent.Type is IntentType.GeneralChat or IntentType.DynamicSkill;
    }

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string? result = await dynamicSkills.TryExecute(chatId, originalText, cancellationToken);
        return string.IsNullOrWhiteSpace(result) ? SkillResult.NotHandled() : SkillResult.Text(result, true);
    }
}
