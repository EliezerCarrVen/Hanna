using System.Text.Json;

namespace Hanna.Lightweight.Services;

public sealed class JsonlStoreService(SecretFilterService secretFilter)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task AppendAsync<T>(string path, T entry, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
        var json = JsonSerializer.Serialize(entry, JsonOptions);
        json = secretFilter.Filter(json);
        await File.AppendAllTextAsync(path, json + Environment.NewLine, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ReadLastLinesAsync(string path, int count, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return [];
        }

        var lines = await File.ReadAllLinesAsync(path, cancellationToken);
        return lines.Where(static line => !string.IsNullOrWhiteSpace(line)).TakeLast(count).ToArray();
    }
}
