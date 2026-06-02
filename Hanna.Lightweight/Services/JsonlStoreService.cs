using System.Text.Json;
using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class JsonlStoreService(SecretFilterService secretFilter, PathGuardService pathGuard, LightweightOptions options, LogRotationService logRotation)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task AppendAsync<T>(string path, T entry, CancellationToken cancellationToken = default)
    {
        var safePath = pathGuard.EnsureInsideRoot(path);
        Directory.CreateDirectory(Path.GetDirectoryName(safePath) ?? pathGuard.Root);
        if (safePath.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
        {
            logRotation.RotateIfNeeded(safePath);
        }

        var json = JsonSerializer.Serialize(entry, JsonOptions);
        json = secretFilter.Filter(options.TruncateMemory(json));
        await File.AppendAllTextAsync(safePath, json + Environment.NewLine, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ReadLastLinesAsync(string path, int count, CancellationToken cancellationToken = default)
    {
        var safePath = pathGuard.EnsureInsideRoot(path);
        if (!File.Exists(safePath))
        {
            return [];
        }

        var cappedCount = Math.Clamp(count, 0, options.MaxJsonlEntriesToRead);
        var lines = await File.ReadAllLinesAsync(safePath, cancellationToken);
        return lines.Where(static line => !string.IsNullOrWhiteSpace(line)).TakeLast(cappedCount).ToArray();
    }

    public int CountLines(string path)
    {
        var safePath = pathGuard.EnsureInsideRoot(path);
        return File.Exists(safePath)
            ? File.ReadLines(safePath).Count(line => !string.IsNullOrWhiteSpace(line))
            : 0;
    }
}
