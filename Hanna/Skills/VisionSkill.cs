using Hanna.Models;
using Hanna.Services;
using Hanna.Spotify;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class VisionSkill : ISkill
{
    private readonly VisionService vision;
    private readonly SpotifySearchService search;
    private readonly SpotifyLibraryService library;
    private readonly SpotifyPlaybackService playback;
    private readonly ResponseService response;

    public VisionSkill(VisionService vision, SpotifySearchService search, SpotifyLibraryService library, SpotifyPlaybackService playback, ResponseService response)
    {
        this.vision = vision;
        this.search = search;
        this.library = library;
        this.playback = playback;
        this.response = response;
    }

    public bool CanHandle(IntentResult intent) => intent.Type == IntentType.Vision;

    public Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        return Task.FromResult(SkillResult.NotHandled());
    }
}
