using YoutubeExplode;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;

namespace Hanna.Services;

internal sealed class YoutubeMediaService
{
    public async Task<string?> Download(string query, long chatId, bool video)
    {
        try
        {
            var youtube = new YoutubeClient();
            VideoSearchResult? selected = null;

            await foreach (var item in youtube.Search.GetVideosAsync(query))
            {
                selected = item;
                break;
            }

            if (selected == null)
                return null;

            var manifest = await youtube.Videos.Streams.GetManifestAsync(selected.Url);

            if (video)
            {
                var streamInfo = manifest.GetMuxedStreams()
                    .Where(s => s.Container == Container.Mp4)
                    .GetWithHighestVideoQuality();

                if (streamInfo == null)
                    return null;

                string path = Path.Combine(Path.GetTempPath(), $"yt_video_{chatId}_{Guid.NewGuid()}.mp4");
                await youtube.Videos.Streams.DownloadAsync(streamInfo, path);
                return path;
            }

            var audioInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            if (audioInfo == null)
                return null;

            string audioPath = Path.Combine(Path.GetTempPath(), $"yt_audio_{chatId}_{Guid.NewGuid()}.m4a");
            await youtube.Videos.Streams.DownloadAsync(audioInfo, audioPath);
            return audioPath;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[YouTube Error]: {ex.Message}");
            return null;
        }
    }
}
