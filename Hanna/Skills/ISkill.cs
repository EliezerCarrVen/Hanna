using Hanna.Models;
using Telegram.Bot;

namespace Hanna.Skills;

internal interface ISkill
{
    bool CanHandle(IntentResult intent);
    Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken);
}
