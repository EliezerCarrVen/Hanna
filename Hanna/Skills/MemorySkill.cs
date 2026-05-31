using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class MemorySkill : ISkill
{
    private readonly MemoryService memory;

    public MemorySkill(MemoryService memory)
    {
        this.memory = memory;
    }

    public bool CanHandle(IntentResult intent) => intent.Type is IntentType.MemorySave or IntentType.MemoryShow;

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (intent.Type == IntentType.MemoryShow)
            return SkillResult.Text(await memory.Read(chatId, cancellationToken), true);

        string value = Regex.Replace(originalText, @"\b(recuerda|guarda en memoria|memoriza|que)\b", " ", RegexOptions.IgnoreCase);
        value = Regex.Replace(value, @"\s+", " ").Trim();

        if (string.IsNullOrWhiteSpace(value))
            return SkillResult.Text("Dime qué quieres que recuerde.", true);

        await memory.Save(chatId, value, cancellationToken);

        return SkillResult.Text("Listo, lo recordaré.", true);
    }
}
