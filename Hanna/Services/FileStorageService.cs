using Hanna.Core;

namespace Hanna.Services;

internal sealed class FileStorageService
{
    private readonly AppConfig config;

    public FileStorageService(AppConfig config)
    {
        this.config = config;
    }

    public string GetChatModePath(long chatId) => Path.Combine(config.SettingsDirectory, $"modo_{chatId}.txt");
    public string GetPreferredDevicePath(long chatId) => Path.Combine(config.SettingsDirectory, $"spotify_device_{chatId}.txt");
    public string GetSpotifyTokenPath(long chatId) => Path.Combine(config.TokensDirectory, $"spotify_refresh_token_{chatId}.txt");
    public string GetLegacySpotifyTokenPath() => Path.Combine(config.BaseDirectory, "spotify_refresh_token.txt");
    public string GetChatContextPath(long chatId) => Path.Combine(config.ContextDirectory, $"chat_{chatId}.txt");
    public string GetModelMemoryPath(long chatId) => Path.Combine(config.MemoryDirectory, $"model_memory_{chatId}.txt");
    public string GetUserPreferencesPath(long chatId) => Path.Combine(config.SettingsDirectory, $"preferences_{chatId}.json");
    public string GetRoutinesPath(long chatId) => Path.Combine(config.SettingsDirectory, $"routines_{chatId}.json");

    public string GetResponseMode(long chatId)
    {
        try
        {
            string path = GetChatModePath(chatId);

            if (!File.Exists(path))
                return "ambos";

            string mode = File.ReadAllText(path, Encoding.UTF8).Trim().ToLowerInvariant();

            return mode is "texto" or "audio" or "ambos" ? mode : "ambos";
        }
        catch
        {
            return "ambos";
        }
    }

    public async Task SetResponseMode(long chatId, string mode, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(GetChatModePath(chatId), mode, Encoding.UTF8, cancellationToken);
    }

    public string GetPreferredDevice(long chatId)
    {
        string path = GetPreferredDevicePath(chatId);
        return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8).Trim() : "";
    }

    public async Task SetPreferredDevice(long chatId, string deviceId, CancellationToken cancellationToken)
    {
        await File.WriteAllTextAsync(GetPreferredDevicePath(chatId), deviceId, Encoding.UTF8, cancellationToken);
    }
}
