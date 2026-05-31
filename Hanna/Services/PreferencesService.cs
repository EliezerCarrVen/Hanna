using Hanna.Core;

namespace Hanna.Services;

internal sealed class PreferencesService
{
    private readonly FileStorageService storage;

    public PreferencesService(FileStorageService storage)
    {
        this.storage = storage;
    }

    public async Task Set(long chatId, string key, string value, CancellationToken cancellationToken)
    {
        var preferences = await Load(chatId, cancellationToken);
        preferences[key] = value;
        await Save(chatId, preferences, cancellationToken);
    }

    public async Task<string> Get(long chatId, string key, CancellationToken cancellationToken)
    {
        var preferences = await Load(chatId, cancellationToken);
        return preferences.TryGetValue(key, out string? value) ? value : "";
    }

    public async Task<string> Show(long chatId, CancellationToken cancellationToken)
    {
        var preferences = await Load(chatId, cancellationToken);

        if (preferences.Count == 0)
            return "Aún no tengo preferencias guardadas para este chat.";

        var sb = new StringBuilder();
        sb.AppendLine("Preferencias guardadas:");

        foreach (var item in preferences)
            sb.AppendLine($"- {item.Key}: {item.Value}");

        return sb.ToString().Trim();
    }

    public async Task<Dictionary<string, string>> Load(long chatId, CancellationToken cancellationToken)
    {
        string path = storage.GetUserPreferencesPath(chatId);

        if (!File.Exists(path))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            string json = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task Save(long chatId, Dictionary<string, string> preferences, CancellationToken cancellationToken)
    {
        string path = storage.GetUserPreferencesPath(chatId);
        string json = JsonSerializer.Serialize(preferences, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, Encoding.UTF8, cancellationToken);
    }
}
