using Hanna.Lightweight.Core;
using Hanna.Lightweight.Models;

namespace Hanna.Lightweight.Services;

public sealed class FlatFileMemoryService(RuntimePaths paths, JsonlStoreService jsonlStore, SecretFilterService secretFilter, LightweightOptions options)
{
    public async Task AddShortMemoryAsync(string source, string content, IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
    {
        var entry = new ShortMemoryEntry(DateTimeOffset.UtcNow, source, secretFilter.Filter(options.TruncateMemory(content)), tags);
        await jsonlStore.AppendAsync(paths.ShortMemory, entry, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ReadRecentShortMemoryAsync(int? count = null, CancellationToken cancellationToken = default) =>
        jsonlStore.ReadLastLinesAsync(paths.ShortMemory, Math.Min(count ?? options.LastEntriesToRead, options.MaxJsonlEntriesToRead), cancellationToken);

    public int CountShortMemoryEntries() => jsonlStore.CountLines(paths.ShortMemory);
}
