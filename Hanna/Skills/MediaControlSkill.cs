using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class MediaControlSkill : ISkill
{
    private readonly MediaControlService media;

    public MediaControlSkill(MediaControlService media)
    {
        this.media = media;
    }

    public bool CanHandle(IntentResult intent)
    {
        return intent.Type is IntentType.MediaNetflixPc or IntentType.MediaNetflixTvLg or IntentType.MediaYoutubeTvLg;
    }

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string response = intent.Type switch
        {
            IntentType.MediaNetflixTvLg => await media.HandleNetflixTvLg(originalText, cancellationToken),
            IntentType.MediaYoutubeTvLg => await media.HandleYoutubeTvLg(originalText, cancellationToken),
            _ => await media.HandleNetflixPc(originalText, cancellationToken)
        };
        return SkillResult.Text(response, true);
    }
}
