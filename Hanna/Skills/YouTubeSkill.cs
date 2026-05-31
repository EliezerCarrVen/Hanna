using Hanna.Models;
using Hanna.Services;
using Hanna.Spotify;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Hanna.Skills;

internal sealed class YouTubeSkill : ISkill
{
    private readonly SpotifySearchService spotifySearch;
    private readonly YoutubeMediaService youtube;
    private readonly ResponseService response;

    public YouTubeSkill(SpotifySearchService spotifySearch, YoutubeMediaService youtube, ResponseService response)
    {
        this.spotifySearch = spotifySearch;
        this.youtube = youtube;
        this.response = response;
    }

    public bool CanHandle(IntentResult intent) => intent.Type is IntentType.YouTubeAudio or IntentType.YouTubeVideo;

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(intent.Query))
            return SkillResult.Text(intent.Type == IntentType.YouTubeVideo ? "Dime qué video quieres descargar." : "Dime qué canción quieres reproducir.");

        await botClient.SendMessage(chatId, intent.Type == IntentType.YouTubeVideo ? $"Buscando video: {intent.Query}" : $"Buscando audio: {intent.Query}", cancellationToken: cancellationToken);

        string query = intent.Query;
        string title = "cancion";
        string artist = "";

        if (intent.Type == IntentType.YouTubeAudio)
        {
            var track = await spotifySearch.SearchTrack(intent.Query, cancellationToken);

            if (track != null && track.Score >= 0.55)
            {
                query = $"{track.Artist} {track.Name}";
                title = track.Name;
                artist = track.Artist;
            }
        }

        string? path = await youtube.Download(query, chatId, intent.Type == IntentType.YouTubeVideo);

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return SkillResult.Text(intent.Type == IntentType.YouTubeVideo ? "No pude enviar el video." : "No pude enviar el audio.");

        await using var stream = File.OpenRead(path);

        if (intent.Type == IntentType.YouTubeVideo)
            await botClient.SendVideo(chatId, InputFile.FromStream(stream, "video.mp4"), cancellationToken: cancellationToken);
        else
            await botClient.SendAudio(chatId, InputFile.FromStream(stream, "cancion.m4a"), title: title, performer: artist, cancellationToken: cancellationToken);

        try { File.Delete(path); } catch { }

        return SkillResult.Silent();
    }
}
