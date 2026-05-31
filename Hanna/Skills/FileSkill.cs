using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class FileSkill : ISkill
{
    private readonly FileControllerService files;

    public FileSkill(FileControllerService files)
    {
        this.files = files;
    }

    public bool CanHandle(IntentResult intent)
    {
        return intent.Type is IntentType.FileList or IntentType.FileRead or IntentType.FileWrite or IntentType.FileFind;
    }

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        return intent.Type switch
        {
            IntentType.FileList => SkillResult.Text(files.ListFiles(originalText), true),
            IntentType.FileRead => SkillResult.Text(await files.ReadFile(originalText, cancellationToken), true),
            IntentType.FileWrite => SkillResult.Text(await files.WriteFile(originalText, cancellationToken), true),
            IntentType.FileFind => SkillResult.Text(files.FindFile(originalText), true),
            _ => SkillResult.NotHandled()
        };
    }
}
