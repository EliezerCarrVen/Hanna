using Hanna.Core;
using Hanna.Services;

namespace Hanna.Spotify;

internal sealed class SpotifyAuthService
{
    private readonly AppConfig config;
    private readonly HttpClient httpClient;
    private readonly FileStorageService storage;

    public SpotifyAuthService(AppConfig config, HttpClient httpClient, FileStorageService storage)
    {
        this.config = config;
        this.httpClient = httpClient;
        this.storage = storage;
    }

    public string BuildAuthUrl(long chatId)
    {
        return "https://accounts.spotify.com/authorize" +
            $"?client_id={Uri.EscapeDataString(config.SpotifyClientId)}" +
            "&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(config.SpotifyRedirectUri)}" +
            $"&scope={Uri.EscapeDataString(config.SpotifyScopes)}" +
            $"&state={chatId}" +
            "&show_dialog=true";
    }

    public async Task<bool> ExchangeCode(long chatId, string code, CancellationToken cancellationToken)
    {
        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.SpotifyClientId}:{config.SpotifyClientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", config.SpotifyRedirectUri)
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[Spotify OAuth Error]: {json}");
            return false;
        }

        using JsonDocument doc = JsonDocument.Parse(json);
        string refreshToken = doc.RootElement.GetProperty("refresh_token").GetString() ?? "";

        if (string.IsNullOrWhiteSpace(refreshToken))
            return false;

        await File.WriteAllTextAsync(storage.GetSpotifyTokenPath(chatId), refreshToken, Encoding.UTF8, cancellationToken);
        await File.WriteAllTextAsync(storage.GetLegacySpotifyTokenPath(), refreshToken, Encoding.UTF8, cancellationToken);

        return true;
    }

    public async Task<string?> GetUserAccessToken(long chatId, CancellationToken cancellationToken)
    {
        MigrateOldToken(chatId);

        string tokenPath = storage.GetSpotifyTokenPath(chatId);

        if (!File.Exists(tokenPath))
            return null;

        string refreshToken = await File.ReadAllTextAsync(tokenPath, Encoding.UTF8, cancellationToken);

        if (string.IsNullOrWhiteSpace(refreshToken))
            return null;

        var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.SpotifyClientId}:{config.SpotifyClientSecret}"));

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);
        request.Content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("refresh_token", refreshToken)
        });

        using var response = await httpClient.SendAsync(request, cancellationToken);
        string json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"[Spotify Refresh Error]: {json}");
            return null;
        }

        using JsonDocument doc = JsonDocument.Parse(json);

        if (doc.RootElement.TryGetProperty("refresh_token", out var newToken))
        {
            string? value = newToken.GetString();
            if (!string.IsNullOrWhiteSpace(value))
                await File.WriteAllTextAsync(tokenPath, value, Encoding.UTF8, cancellationToken);
        }

        return doc.RootElement.GetProperty("access_token").GetString();
    }

    public bool HasToken(long chatId)
    {
        MigrateOldToken(chatId);
        return File.Exists(storage.GetSpotifyTokenPath(chatId));
    }

    public void Reset(long chatId)
    {
        try
        {
            if (File.Exists(storage.GetSpotifyTokenPath(chatId)))
                File.Delete(storage.GetSpotifyTokenPath(chatId));

            if (File.Exists(storage.GetLegacySpotifyTokenPath()))
                File.Delete(storage.GetLegacySpotifyTokenPath());
        }
        catch
        {
        }
    }

    public static string ExtractCode(string text)
    {
        string clean = Regex.Replace(text.Trim(), @"^/auth\s*", "", RegexOptions.IgnoreCase).Trim();

        if (string.IsNullOrWhiteSpace(clean))
            return "";

        if (Uri.TryCreate(clean, UriKind.Absolute, out var uri))
        {
            string query = uri.Query.TrimStart('?');

            foreach (string parameter in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = parameter.Split('=', 2);
                if (parts.Length == 2 && parts[0].Equals("code", StringComparison.OrdinalIgnoreCase))
                    return Uri.UnescapeDataString(parts[1]);
            }
        }

        var match = Regex.Match(clean, @"(?:code=)([^&\s]+)", RegexOptions.IgnoreCase);

        if (match.Success)
            return Uri.UnescapeDataString(match.Groups[1].Value.Trim());

        return clean;
    }

    private void MigrateOldToken(long chatId)
    {
        try
        {
            string newPath = storage.GetSpotifyTokenPath(chatId);
            string oldPath = storage.GetLegacySpotifyTokenPath();

            if (!File.Exists(newPath) && File.Exists(oldPath))
                File.Copy(oldPath, newPath, false);
        }
        catch
        {
        }
    }
}
