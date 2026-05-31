using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class ReminderSkill : ISkill
{
    private readonly ReminderService reminders;

    public ReminderSkill(ReminderService reminders)
    {
        this.reminders = reminders;
    }

    public bool CanHandle(IntentResult intent) => intent.Type is IntentType.ReminderSet or IntentType.ReminderList;

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (intent.Type == IntentType.ReminderList)
            return SkillResult.Text(await reminders.ListReminders(chatId, cancellationToken), true);

        return SkillResult.Text(await reminders.CreateReminder(botClient, chatId, originalText, cancellationToken), true);
    }
}
