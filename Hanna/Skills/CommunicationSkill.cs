using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class CommunicationSkill : ISkill
{
    private readonly CommunicationService communication;

    public CommunicationSkill(CommunicationService communication)
    {
        this.communication = communication;
    }

    public bool CanHandle(IntentResult intent) => intent.Type == IntentType.SendMessage;

    public Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        return Task.FromResult(SkillResult.Text(communication.SendMessagePlaceholder(originalText), true));
    }
}
