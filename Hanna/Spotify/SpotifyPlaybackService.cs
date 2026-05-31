using Hanna.Core;
using Hanna.Models;
using Hanna.Services;

namespace Hanna.Spotify;

internal sealed class SpotifyPlaybackService
{
    private readonly AppConfig config;
    private readonly HttpClient httpClient;
    private readonly SpotifyAuthService auth;
    private readonly FileStorageService storage;

    public SpotifyPlaybackService(AppConfig config, HttpClient httpClient, SpotifyAuthService auth, FileStorageService storage)
    {
        this.config = config;
        this.httpClient = httpClient;
        this.auth = auth;
        this.storage = storage;
    }

    public async Task<List<SpotifyDevice>> GetDevices(long chatId, CancellationToken cancellationToken)
    {
        var list = new List<SpotifyDevice>();
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return list;

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/player/devices");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[Spotify Devices Error]: {json}");
            return list;
        }

        using JsonDocument doc = JsonDocument.Parse(json);

        foreach (var item in doc.RootElement.GetProperty("devices").EnumerateArray())
        {
            list.Add(new SpotifyDevice
            {
                Id = item.GetProperty("id").GetString() ?? "",
                Name = item.GetProperty("name").GetString() ?? "",
                Type = item.GetProperty("type").GetString() ?? "",
                IsActive = item.GetProperty("is_active").GetBoolean()
            });
        }

        return list;
    }

    public async Task<SpotifyDevice?> SelectDevice(long chatId, string requestedDevice, CancellationToken cancellationToken)
    {
        var devices = await GetDevices(chatId, cancellationToken);

        if (devices.Count == 0)
            return null;

        if (!string.IsNullOrWhiteSpace(requestedDevice))
        {
            string norm = Utilities.TextTools.Normalize(requestedDevice);

            var requested = devices.FirstOrDefault(d =>
                Utilities.TextTools.Normalize(d.Name).Contains(norm) ||
                Utilities.TextTools.Normalize(d.Type).Contains(norm) ||
                norm.Contains(Utilities.TextTools.Normalize(d.Type)));

            if (requested != null)
                return requested;
        }

        string preferred = storage.GetPreferredDevice(chatId);

        if (!string.IsNullOrWhiteSpace(preferred))
        {
            var preferredDevice = devices.FirstOrDefault(d =>
                d.Id == preferred ||
                d.Name.Equals(preferred, StringComparison.OrdinalIgnoreCase));

            if (preferredDevice != null)
                return preferredDevice;
        }

        return devices.FirstOrDefault(d => d.IsActive) ?? devices.FirstOrDefault();
    }

    public async Task<bool> SetPreferredDeviceByIndex(long chatId, int index, CancellationToken cancellationToken)
    {
        var devices = await GetDevices(chatId, cancellationToken);

        if (index < 1 || index > devices.Count)
            return false;

        await storage.SetPreferredDevice(chatId, devices[index - 1].Id, cancellationToken);
        return true;
    }

    public async Task<SpotifyOperationResult> Transfer(long chatId, string deviceId, bool play, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return new SpotifyOperationResult(false, 0, "No se pudo renovar el access token.");

        var payload = new
        {
            device_ids = new[] { deviceId },
            play
        };

        using var request = new HttpRequestMessage(HttpMethod.Put, "https://api.spotify.com/v1/me/player");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new SpotifyOperationResult(response.IsSuccessStatusCode, (int)response.StatusCode, Utilities.TextTools.Clip(body, 500));
    }

    public async Task<SpotifyOperationResult> PlayTrack(long chatId, string trackId, string requestedDevice, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return new SpotifyOperationResult(false, 0, "No se pudo renovar el access token.");

        SpotifyDevice? device = await SelectDevice(chatId, requestedDevice, cancellationToken);

        if (device == null)
            return new SpotifyOperationResult(false, 404, "NO_ACTIVE_DEVICE");

        await Transfer(chatId, device.Id, true, cancellationToken);
        await Task.Delay(350, cancellationToken);

        var payload = new
        {
            uris = new[] { $"spotify:track:{trackId}" }
        };

        string url = $"https://api.spotify.com/v1/me/player/play?device_id={Uri.EscapeDataString(device.Id)}";

        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new SpotifyOperationResult(response.IsSuccessStatusCode, (int)response.StatusCode, Utilities.TextTools.Clip(body, 500));
    }
    public async Task<SpotifyOperationResult> PlayPlaylist(long chatId, string playlistId, string requestedDevice, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return new SpotifyOperationResult(false, 0, "No se pudo renovar el access token.");

        SpotifyDevice? device = await SelectDevice(chatId, requestedDevice, cancellationToken);

        if (device == null)
            return new SpotifyOperationResult(false, 404, "NO_ACTIVE_DEVICE");

        await Transfer(chatId, device.Id, true, cancellationToken);
        await Task.Delay(350, cancellationToken);

        var payload = new
        {
            context_uri = $"spotify:playlist:{playlistId}"
        };

        string url = $"https://api.spotify.com/v1/me/player/play?device_id={Uri.EscapeDataString(device.Id)}";

        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new SpotifyOperationResult(
            response.IsSuccessStatusCode,
            (int)response.StatusCode,
            Utilities.TextTools.Clip(body, 500));
    }


    public async Task<SpotifyOperationResult> PlayAlbum(long chatId, string albumId, string requestedDevice, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return new SpotifyOperationResult(false, 0, "No se pudo renovar el access token.");

        SpotifyDevice? device = await SelectDevice(chatId, requestedDevice, cancellationToken);

        if (device == null)
            return new SpotifyOperationResult(false, 404, "NO_ACTIVE_DEVICE");

        await Transfer(chatId, device.Id, true, cancellationToken);
        await Task.Delay(350, cancellationToken);

        var payload = new
        {
            context_uri = $"spotify:album:{albumId}"
        };

        string url = $"https://api.spotify.com/v1/me/player/play?device_id={Uri.EscapeDataString(device.Id)}";

        using var request = new HttpRequestMessage(HttpMethod.Put, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new SpotifyOperationResult(
            response.IsSuccessStatusCode,
            (int)response.StatusCode,
            Utilities.TextTools.Clip(body, 500));
    }

    public async Task<SpotifyOperationResult> AddToQueue(long chatId, string trackId, string requestedDevice, CancellationToken cancellationToken)
    {
        return await AddUriToQueue(chatId, $"spotify:track:{trackId}", requestedDevice, cancellationToken);
    }

    public async Task<SpotifyOperationResult> AddAlbumToQueue(long chatId, string albumId, string requestedDevice, CancellationToken cancellationToken)
    {
        var uris = await GetAlbumTrackUris(chatId, albumId, cancellationToken);

        if (uris.Count == 0)
            return new SpotifyOperationResult(false, 404, "No encontré canciones dentro del álbum.");

        return await AddManyUrisToQueue(chatId, uris, requestedDevice, cancellationToken);
    }

    public async Task<SpotifyOperationResult> AddPlaylistToQueue(long chatId, string playlistId, string requestedDevice, CancellationToken cancellationToken)
    {
        var uris = await GetPlaylistTrackUris(chatId, playlistId, cancellationToken);

        if (uris.Count == 0)
            return new SpotifyOperationResult(false, 404, "No encontré canciones dentro de la playlist.");

        return await AddManyUrisToQueue(chatId, uris, requestedDevice, cancellationToken);
    }

    public async Task<string> GetQueue(long chatId, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return "No pude renovar tu sesión de Spotify.";

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.spotify.com/v1/me/player/queue");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            return $"No pude leer la fila de Spotify. Código: {(int)response.StatusCode}. Detalle: {Utilities.TextTools.Clip(json, 300)}";

        using JsonDocument doc = JsonDocument.Parse(json);

        var sb = new StringBuilder();

        if (doc.RootElement.TryGetProperty("currently_playing", out var current) &&
            current.ValueKind != JsonValueKind.Null)
        {
            string currentName = current.TryGetProperty("name", out var cn) ? cn.GetString() ?? "Sin título" : "Sin título";
            string currentArtist = TryGetFirstArtist(current);
            sb.AppendLine($"Sonando ahora: {currentName} - {currentArtist}");
        }

        if (doc.RootElement.TryGetProperty("queue", out var queue) &&
            queue.ValueKind == JsonValueKind.Array &&
            queue.GetArrayLength() > 0)
        {
            sb.AppendLine("Fila de reproducción:");

            int i = 1;
            foreach (var item in queue.EnumerateArray().Take(10))
            {
                string name = item.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "Sin título" : "Sin título";
                string artist = TryGetFirstArtist(item);
                sb.AppendLine($"{i}. {name} - {artist}");
                i++;
            }
        }
        else
        {
            sb.AppendLine("La fila está vacía o Spotify no la está reportando.");
        }

        return sb.ToString().Trim();
    }

    public Task<SpotifyOperationResult> Pause(long chatId, CancellationToken cancellationToken)
        => PlayerCommand(chatId, HttpMethod.Put, "https://api.spotify.com/v1/me/player/pause", cancellationToken);

    public Task<SpotifyOperationResult> Resume(long chatId, CancellationToken cancellationToken)
        => PlayerCommand(chatId, HttpMethod.Put, "https://api.spotify.com/v1/me/player/play", cancellationToken);

    public Task<SpotifyOperationResult> Next(long chatId, CancellationToken cancellationToken)
        => PlayerCommand(chatId, HttpMethod.Post, "https://api.spotify.com/v1/me/player/next", cancellationToken);

    public Task<SpotifyOperationResult> Previous(long chatId, CancellationToken cancellationToken)
        => PlayerCommand(chatId, HttpMethod.Post, "https://api.spotify.com/v1/me/player/previous", cancellationToken);

    private async Task<SpotifyOperationResult> AddUriToQueue(long chatId, string uri, string requestedDevice, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return new SpotifyOperationResult(false, 0, "No se pudo renovar el access token.");

        SpotifyDevice? device = await SelectDevice(chatId, requestedDevice, cancellationToken);

        if (device == null)
            return new SpotifyOperationResult(false, 404, "NO_ACTIVE_DEVICE");

        string url =
            "https://api.spotify.com/v1/me/player/queue" +
            $"?uri={Uri.EscapeDataString(uri)}" +
            $"&device_id={Uri.EscapeDataString(device.Id)}";

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new SpotifyOperationResult(response.IsSuccessStatusCode, (int)response.StatusCode, Utilities.TextTools.Clip(body, 500));
    }

    private async Task<SpotifyOperationResult> AddManyUrisToQueue(long chatId, List<string> uris, string requestedDevice, CancellationToken cancellationToken)
    {
        int added = 0;
        SpotifyOperationResult last = new(true, 200, "");

        foreach (string uri in uris.Take(100))
        {
            last = await AddUriToQueue(chatId, uri, requestedDevice, cancellationToken);

            if (!last.Success)
                return new SpotifyOperationResult(false, last.StatusCode, $"Agregadas: {added}. Error: {last.Detail}");

            added++;
            await Task.Delay(120, cancellationToken);
        }

        return new SpotifyOperationResult(true, 200, added.ToString());
    }

    private async Task<List<string>> GetAlbumTrackUris(long chatId, string albumId, CancellationToken cancellationToken)
    {
        var uris = new List<string>();
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return uris;

        string url = $"https://api.spotify.com/v1/albums/{Uri.EscapeDataString(albumId)}/tracks?limit=50&market=MX";

        while (!string.IsNullOrWhiteSpace(url))
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                break;

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using JsonDocument doc = JsonDocument.Parse(json);

            foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
            {
                string uri = item.GetProperty("uri").GetString() ?? "";

                if (!string.IsNullOrWhiteSpace(uri))
                    uris.Add(uri);
            }

            url = doc.RootElement.TryGetProperty("next", out var next) && next.ValueKind != JsonValueKind.Null
                ? next.GetString() ?? ""
                : "";
        }

        return uris;
    }

    private async Task<List<string>> GetPlaylistTrackUris(long chatId, string playlistId, CancellationToken cancellationToken)
    {
        var uris = new List<string>();
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return uris;

        string url = $"https://api.spotify.com/v1/playlists/{Uri.EscapeDataString(playlistId)}/tracks?limit=100&market=MX";

        while (!string.IsNullOrWhiteSpace(url) && uris.Count < 100)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
                break;

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            using JsonDocument doc = JsonDocument.Parse(json);

            foreach (var item in doc.RootElement.GetProperty("items").EnumerateArray())
            {
                if (!item.TryGetProperty("track", out var track))
                    continue;

                if (track.ValueKind == JsonValueKind.Null)
                    continue;

                string type = track.TryGetProperty("type", out var typeEl) ? typeEl.GetString() ?? "" : "";

                if (!type.Equals("track", StringComparison.OrdinalIgnoreCase))
                    continue;

                string uri = track.GetProperty("uri").GetString() ?? "";

                if (!string.IsNullOrWhiteSpace(uri))
                    uris.Add(uri);
            }

            url = doc.RootElement.TryGetProperty("next", out var next) && next.ValueKind != JsonValueKind.Null
                ? next.GetString() ?? ""
                : "";
        }

        return uris;
    }

    private async Task<SpotifyOperationResult> PlayerCommand(long chatId, HttpMethod method, string url, CancellationToken cancellationToken)
    {
        string? token = await auth.GetUserAccessToken(chatId, cancellationToken);

        if (string.IsNullOrWhiteSpace(token))
            return new SpotifyOperationResult(false, 0, "No se pudo renovar el access token.");

        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new SpotifyOperationResult(response.IsSuccessStatusCode, (int)response.StatusCode, Utilities.TextTools.Clip(body, 500));
    }

    private static string TryGetFirstArtist(JsonElement item)
    {
        try
        {
            if (item.TryGetProperty("artists", out var artists) &&
                artists.ValueKind == JsonValueKind.Array &&
                artists.GetArrayLength() > 0)
            {
                return artists[0].TryGetProperty("name", out var name)
                    ? name.GetString() ?? "Artista desconocido"
                    : "Artista desconocido";
            }
        }
        catch
        {
        }

        return "Artista desconocido";
    }
}