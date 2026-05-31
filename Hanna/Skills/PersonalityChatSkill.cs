using Hanna.Models;
using Hanna.Services;
using Hanna.Utilities;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class PersonalityChatSkill : ISkill
{
    private readonly PromptPackService prompts;

    public PersonalityChatSkill(PromptPackService prompts)
    {
        this.prompts = prompts;
    }

    public bool CanHandle(IntentResult intent) => intent.Type == IntentType.PersonalityModify;

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string note = originalText.Trim();

        note = Regex.Replace(note, @"\b(hanna|recuerda|guarda|modifica|cambia|añade|agrega|personalidad|mi personalidad|tu personalidad)\b", "", RegexOptions.IgnoreCase).Trim();

        if (string.IsNullOrWhiteSpace(note))
            note = originalText.Trim();

        await prompts.AppendChatPersonality(chatId, note, cancellationToken);

        return SkillResult.Text("Listo. Guardé ese ajuste de personalidad solo para este chat.");
    }
}
