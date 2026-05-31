using Hanna.Models;

namespace Hanna.Spotify;

internal sealed class SpotifyLibraryService
{
    private readonly HttpClient httpClient;
    private readonly SpotifyAuthService auth;

    public SpotifyLibraryService(HttpClient httpClient, SpotifyAuthService auth)
    {
        this.httpClient = httpClient;
        this.auth = auth;
    }

    public async Task<SpotifyOperationResult> AddToLiked(long chatId, string trackId, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
            return new SpotifyOperationResult(false, 0, "No se pudo renovar el access token.");

        using var request = new HttpRequestMessage(HttpMethod.Put, $"https://api.spotify.com/v1/me/tracks?ids={Uri.EscapeDataString(trackId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new SpotifyOperationResult(response.IsSuccessStatusCode, (int)response.StatusCode, Utilities.TextTools.Clip(body, 300));
    }

    public async Task<bool> IsLiked(long chatId, string trackId, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);
        if (string.IsNullOrWhiteSpace(token)) return false;

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/me/tracks/contains?ids={Uri.EscapeDataString(trackId)}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return false;

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        using JsonDocument doc = JsonDocument.Parse(json);

        return doc.RootElement.GetArrayLength() > 0 && doc.RootElement[0].GetBoolean();
    }

    public async Task<string> ListLiked(long chatId, int limit, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
            return "No pude renovar tu sesión de Spotify. Escribe /auth otra vez.";

        limit = Math.Clamp(limit, 1, 50);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.spotify.com/v1/me/tracks?limit={limit}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return $"No pude leer tus Me gusta de Spotify. Código: {(int)response.StatusCode}. Detalle: {Utilities.TextTools.Clip(json, 300)}";

        using var doc = JsonDocument.Parse(json);
        var items = doc.RootElement.GetProperty("items");

        if (items.GetArrayLength() == 0)
            return "No encontré canciones en tus Me gusta de Spotify.";

        var sb = new StringBuilder();
        sb.AppendLine($"Tus primeros {items.GetArrayLength()} Me gusta de Spotify:");

        int i = 1;
        foreach (var item in items.EnumerateArray())
        {
            var track = item.GetProperty("track");
            string name = track.GetProperty("name").GetString() ?? "Sin título";
            string artist = track.GetProperty("artists")[0].GetProperty("name").GetString() ?? "Artista desconocido";
            sb.AppendLine($"{i}. {name} - {artist}");
            i++;
        }

        return sb.ToString().Trim();
    }

    public async Task<string?> GetCurrentUserId(long chatId, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);
        if (string.IsNullOrWhiteSpace(token)) return null;

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode) return null;

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        using JsonDocument doc = JsonDocument.Parse(json);

        return doc.RootElement.TryGetProperty("id", out var id) ? id.GetString() : null;
    }

    public async Task<List<SpotifyPlaylistInfo>> GetPlaylists(long chatId, CancellationToken cancellationToken)
    {
        var list = new List<SpotifyPlaylistInfo>();
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);
        if (string.IsNullOrWhiteSpace(token)) return list;

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/playlists?limit=50");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[Spotify Playlists Error]: {json}");
            return list;
        }

        using JsonDocument doc = JsonDocument.Parse(json);

        foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
        {
            list.Add(new SpotifyPlaylistInfo
            {
                Id = item.GetProperty("id").GetString() ?? "",
                Name = item.GetProperty("name").GetString() ?? "",
                Public = item.TryGetProperty("public", out var pub) && pub.ValueKind != JsonValueKind.Null && pub.GetBoolean(),
                TracksTotal = item.GetProperty("tracks").GetProperty("total").GetInt32()
            });
        }

        return list;
    }

    public async Task<SpotifyPlaylistInfo?> FindPlaylist(long chatId, string playlistName, CancellationToken cancellationToken)
    {
        var playlists = await GetPlaylists(chatId, cancellationToken);
        string normalized = Utilities.TextTools.Normalize(playlistName);

        return playlists.FirstOrDefault(p => Utilities.TextTools.Normalize(p.Name) == normalized)
            ?? playlists.FirstOrDefault(p => Utilities.TextTools.Normalize(p.Name).Contains(normalized))
            ?? playlists.FirstOrDefault(p => normalized.Contains(Utilities.TextTools.Normalize(p.Name)));
    }

    public async Task<SpotifyPlaylistInfo?> CreatePlaylist(long chatId, string playlistName, bool isPublic, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);
        string? userId = await GetCurrentUserId(chatId, cancellationToken);

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(userId))
            return null;

        var payload = new
        {
            name = playlistName,
            @public = isPublic,
            description = "Creada por Hanna."
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.spotify.com/v1/users/{Uri.EscapeDataString(userId)}/playlists");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[Spotify Create Playlist Error]: {json}");
            return null;
        }

        using JsonDocument doc = JsonDocument.Parse(json);

        return new SpotifyPlaylistInfo
        {
            Id = doc.RootElement.GetProperty("id").GetString() ?? "",
            Name = doc.RootElement.GetProperty("name").GetString() ?? playlistName
        };
    }

    public async Task<SpotifyOperationResult> AddTrackToPlaylist(long chatId, string playlistId, string trackId, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);
        if (string.IsNullOrWhiteSpace(token))
            return new SpotifyOperationResult(false, 0, "No se pudo renovar el access token.");

        var payload = new { uris = new[] { $"spotify:track:{trackId}" } };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"https://api.spotify.com/v1/playlists/{Uri.EscapeDataString(playlistId)}/tracks");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new SpotifyOperationResult(response.IsSuccessStatusCode, (int)response.StatusCode, Utilities.TextTools.Clip(body, 500));
    }

    public async Task<string> ListPlaylists(long chatId, CancellationToken cancellationToken)
    {
        var playlists = await GetPlaylists(chatId, cancellationToken);

        if (playlists.Count == 0)
            return "No encontré playlists en tu Spotify.";

        var sb = new StringBuilder();
        sb.AppendLine("Tus playlists de Spotify:");

        int i = 1;
        foreach (var playlist in playlists.Take(20))
        {
            sb.AppendLine($"{i}. {playlist.Name} ({playlist.TracksTotal} canciones)");
            i++;
        }

        return sb.ToString().Trim();
    }

    public async Task<List<SpotifyTrack>> GetLikedTracks(long chatId, int limit, CancellationToken cancellationToken)
    {
        var list = new List<SpotifyTrack>();
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return list;

        limit = Math.Clamp(limit, 1, 50);
        string url = $"https://api.spotify.com/v1/me/tracks?limit={limit}&market=MX";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[Spotify Liked Tracks Error]: {json}");
            return list;
        }

        using JsonDocument doc = JsonDocument.Parse(json);

        foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
        {
            if (!item.TryGetProperty("track", out var track) || track.ValueKind == JsonValueKind.Null)
                continue;

            string id = track.GetProperty("id").GetString() ?? "";
            string name = track.GetProperty("name").GetString() ?? "";
            string artist = track.GetProperty("artists")[0].GetProperty("name").GetString() ?? "";
            string album = track.GetProperty("album").GetProperty("name").GetString() ?? "";

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                continue;

            list.Add(new SpotifyTrack
            {
                Id = id,
                Name = name,
                Artist = artist,
                Album = album,
                Score = 0
            });
        }

        return list;
    }

}
