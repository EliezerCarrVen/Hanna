using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class AgentCodingSkill : ISkill
{
    private readonly AgentCodingService coding;

    public AgentCodingSkill(AgentCodingService coding)
    {
        this.coding = coding;
    }

    public bool CanHandle(IntentResult intent) => intent.Type == IntentType.AgentCode;

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        var result = await coding.GenerateCodeFromRequest(chatId, originalText, cancellationToken);

        string response = result.Response;
        if (!string.IsNullOrWhiteSpace(result.FilePath))
            response += "\n\nArchivo generado: " + result.FilePath;

        return SkillResult.Text(response, true);
    }
}
