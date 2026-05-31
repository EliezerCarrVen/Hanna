using Hanna.Core;
using Hanna.Models;
using Hanna.Utilities;

namespace Hanna.Spotify;

internal sealed class SpotifySearchService
{
    private readonly AppConfig config;
    private readonly HttpClient httpClient;

    public SpotifySearchService(AppConfig config, HttpClient httpClient)
    {
        this.config = config;
        this.httpClient = httpClient;
    }

    public async Task<SpotifyTrack?> SearchTrack(string query, CancellationToken cancellationToken)
    {
        string? token = await GetClientToken(cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return null;

        var candidates = new List<SpotifyTrack>();

        foreach (string q in TextTools.CreateSpotifyQueries(query))
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(q)}&type=track&limit=5&market=MX");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                continue;

            string json = await response.Content.ReadAsStringAsync(cancellationToken);

            using JsonDocument doc = JsonDocument.Parse(json);
            var items = doc.RootElement.GetProperty("tracks").GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                string id = item.GetProperty("id").GetString() ?? "";

                if (candidates.Any(c => c.Id == id))
                    continue;

                string name = item.GetProperty("name").GetString() ?? "";
                string artist = item.GetProperty("artists")[0].GetProperty("name").GetString() ?? "";
                string album = item.GetProperty("album").GetProperty("name").GetString() ?? "";

                candidates.Add(new SpotifyTrack
                {
                    Id = id,
                    Name = name,
                    Artist = artist,
                    Album = album,
                    Score = TextTools.ScoreSpotify(query, name, artist)
                });
            }
        }

        var best = candidates.OrderByDescending(c => c.Score).FirstOrDefault();

        if (best != null)
            Console.WriteLine($"[Spotify Track] Query: {query} | Mejor: {best.Name} - {best.Artist} | Score: {best.Score:0.00}");

        return best;
    }

    public async Task<SpotifyCatalogItem?> SearchAlbum(string query, CancellationToken cancellationToken)
    {
        string? token = await GetClientToken(cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return null;

        var candidates = new List<SpotifyCatalogItem>();

        foreach (string q in TextTools.CreateSpotifyQueries(query))
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(q)}&type=album&limit=5&market=MX");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                continue;

            string json = await response.Content.ReadAsStringAsync(cancellationToken);

            using JsonDocument doc = JsonDocument.Parse(json);
            var items = doc.RootElement.GetProperty("albums").GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                string id = item.GetProperty("id").GetString() ?? "";

                if (candidates.Any(c => c.Id == id))
                    continue;

                string name = item.GetProperty("name").GetString() ?? "";
                string artist = item.GetProperty("artists")[0].GetProperty("name").GetString() ?? "";
                string uri = item.GetProperty("uri").GetString() ?? "";
                int total = item.TryGetProperty("total_tracks", out var totalEl) ? totalEl.GetInt32() : 0;

                candidates.Add(new SpotifyCatalogItem
                {
                    Id = id,
                    Uri = uri,
                    Name = name,
                    Artist = artist,
                    Type = "album",
                    TracksTotal = total,
                    Score = TextTools.ScoreSpotify(query, name, artist)
                });
            }
        }

        var best = candidates.OrderByDescending(c => c.Score).FirstOrDefault();

        if (best != null)
            Console.WriteLine($"[Spotify Album] Query: {query} | Mejor: {best.Name} - {best.Artist} | Score: {best.Score:0.00}");

        return best;
    }

    public async Task<SpotifyCatalogItem?> SearchPlaylist(string query, CancellationToken cancellationToken)
    {
        string? token = await GetClientToken(cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return null;

        var candidates = new List<SpotifyCatalogItem>();

        foreach (string q in TextTools.CreateSpotifyQueries(query))
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.spotify.com/v1/search?q={Uri.EscapeDataString(q)}&type=playlist&limit=5&market=MX");

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                continue;

            string json = await response.Content.ReadAsStringAsync(cancellationToken);

            using JsonDocument doc = JsonDocument.Parse(json);
            var items = doc.RootElement.GetProperty("playlists").GetProperty("items");

            foreach (var item in items.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Null)
                    continue;

                string id = item.GetProperty("id").GetString() ?? "";

                if (string.IsNullOrWhiteSpace(id) || candidates.Any(c => c.Id == id))
                    continue;

                string name = item.GetProperty("name").GetString() ?? "";
                string uri = item.GetProperty("uri").GetString() ?? "";
                int total = item.TryGetProperty("tracks", out var tracksEl) &&
                            tracksEl.TryGetProperty("total", out var totalEl)
                    ? totalEl.GetInt32()
                    : 0;

                candidates.Add(new SpotifyCatalogItem
                {
                    Id = id,
                    Uri = uri,
                    Name = name,
                    Artist = "",
                    Type = "playlist",
                    TracksTotal = total,
                    Score = TextTools.ScoreSpotify(query, name, "")
                });
            }
        }

        var best = candidates.OrderByDescending(c => c.Score).FirstOrDefault();

        if (best != null)
            Console.WriteLine($"[Spotify Playlist] Query: {query} | Mejor: {best.Name} | Score: {best.Score:0.00}");

        return best;
    }

    private async Task<string?> GetClientToken(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.SpotifyClientId) || string.IsNullOrWhiteSpace(config.SpotifyClientSecret))
            return null;

        string authHeader = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{config.SpotifyClientId}:{config.SpotifyClientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials")
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        using JsonDocument doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("access_token").GetString();
    }
}