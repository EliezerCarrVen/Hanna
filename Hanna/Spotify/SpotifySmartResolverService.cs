using Hanna.Models;
using Hanna.Utilities;

namespace Hanna.Spotify;

internal sealed class SpotifySmartResolverService
{
    private readonly SpotifySearchService search;
    private readonly SpotifyLibraryService library;

    public SpotifySmartResolverService(SpotifySearchService search, SpotifyLibraryService library)
    {
        this.search = search;
        this.library = library;
    }

    public async Task<SpotifySmartMatch?> ResolveForPlay(long chatId, string originalText, CancellationToken cancellationToken)
    {
        string normalizedOriginal = TextTools.Normalize(TextTools.NormalizeSpotifySpeech(originalText));
        string query = TextTools.ExtractSpotifyPlayQuery(originalText);

        if (string.IsNullOrWhiteSpace(query))
            return null;

        bool wantsPlaylist = Regex.IsMatch(normalizedOriginal, @"\b(mi playlist|playlist|play list|lista de reproduccion|lista de reproducción)\b");
        bool wantsAlbum = Regex.IsMatch(normalizedOriginal, @"\b(album|álbum|disco)\b");
        bool wantsTrack = Regex.IsMatch(normalizedOriginal, @"\b(cancion|canción|track|tema)\b");

        var candidates = new List<SpotifySmartMatch>();

        await AddUserPlaylistCandidates(chatId, query, candidates, cancellationToken);
        await AddLikedTrackCandidates(chatId, query, candidates, cancellationToken);
        await AddPublicCandidates(query, candidates, cancellationToken);

        if (candidates.Count == 0)
            return null;

        foreach (var c in candidates)
        {
            if (wantsPlaylist && c.Type == SpotifySmartType.Playlist)
                c.Score += 0.18;
            if (wantsAlbum && c.Type == SpotifySmartType.Album)
                c.Score += 0.18;
            if (wantsTrack && c.Type == SpotifySmartType.Track)
                c.Score += 0.12;
            if (c.Source == SpotifySmartSource.UserLibrary)
                c.Score += 0.08;
        }

        var best = candidates
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Type == SpotifySmartType.Playlist ? 0 : c.Type == SpotifySmartType.Album ? 1 : 2)
            .FirstOrDefault();

        if (best != null)
        {
            Console.WriteLine($"[Spotify Smart] Query: {query} | Mejor: {best.Type} {best.Name} {best.Artist} | Source: {best.Source} | Score: {best.Score:0.00}");
        }

        return best;
    }

    private async Task AddUserPlaylistCandidates(long chatId, string query, List<SpotifySmartMatch> candidates, CancellationToken cancellationToken)
    {
        try
        {
            var playlists = await library.GetPlaylists(chatId, cancellationToken);

            foreach (var playlist in playlists)
            {
                double score = TextTools.ScoreSpotify(query, playlist.Name, "");

                if (score < 0.35)
                    continue;

                candidates.Add(new SpotifySmartMatch
                {
                    Id = playlist.Id,
                    Name = playlist.Name,
                    Artist = "",
                    Type = SpotifySmartType.Playlist,
                    Source = SpotifySmartSource.UserLibrary,
                    Score = score,
                    TracksTotal = playlist.TracksTotal
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Spotify Smart Playlists Error]: " + ex.Message);
        }
    }

    private async Task AddLikedTrackCandidates(long chatId, string query, List<SpotifySmartMatch> candidates, CancellationToken cancellationToken)
    {
        try
        {
            var tracks = await library.GetLikedTracks(chatId, 50, cancellationToken);

            foreach (var track in tracks)
            {
                double score = TextTools.ScoreSpotify(query, track.Name, track.Artist);

                if (score < 0.35)
                    continue;

                candidates.Add(new SpotifySmartMatch
                {
                    Id = track.Id,
                    Name = track.Name,
                    Artist = track.Artist,
                    Type = SpotifySmartType.Track,
                    Source = SpotifySmartSource.UserLibrary,
                    Score = score,
                    TracksTotal = 0
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Spotify Smart Liked Error]: " + ex.Message);
        }
    }

    private async Task AddPublicCandidates(string query, List<SpotifySmartMatch> candidates, CancellationToken cancellationToken)
    {
        try
        {
            var track = await search.SearchTrack(query, cancellationToken);
            if (track != null)
            {
                candidates.Add(new SpotifySmartMatch
                {
                    Id = track.Id,
                    Name = track.Name,
                    Artist = track.Artist,
                    Type = SpotifySmartType.Track,
                    Source = SpotifySmartSource.PublicSearch,
                    Score = track.Score,
                    TracksTotal = 0
                });
            }
        }
        catch { }

        try
        {
            var album = await search.SearchAlbum(query, cancellationToken);
            if (album != null)
            {
                candidates.Add(new SpotifySmartMatch
                {
                    Id = album.Id,
                    Name = album.Name,
                    Artist = album.Artist,
                    Type = SpotifySmartType.Album,
                    Source = SpotifySmartSource.PublicSearch,
                    Score = album.Score,
                    TracksTotal = album.TracksTotal
                });
            }
        }
        catch { }

        try
        {
            var playlist = await search.SearchPlaylist(query, cancellationToken);
            if (playlist != null)
            {
                candidates.Add(new SpotifySmartMatch
                {
                    Id = playlist.Id,
                    Name = playlist.Name,
                    Artist = "",
                    Type = SpotifySmartType.Playlist,
                    Source = SpotifySmartSource.PublicSearch,
                    Score = playlist.Score,
                    TracksTotal = playlist.TracksTotal
                });
            }
        }
        catch { }
    }
}

internal sealed class SpotifySmartMatch
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Artist { get; set; } = "";
    public SpotifySmartType Type { get; set; }
    public SpotifySmartSource Source { get; set; }
    public double Score { get; set; }
    public int TracksTotal { get; set; }
}

internal enum SpotifySmartType
{
    Track,
    Album,
    Playlist
}

internal enum SpotifySmartSource
{
    UserLibrary,
    PublicSearch
}
