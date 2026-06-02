using Hanna.Lightweight.Core;
using Hanna.Lightweight.Models;

namespace Hanna.Lightweight.Services;

public sealed class FlatFileMemoryService(RuntimePaths paths, JsonlStoreService jsonlStore, SecretFilterService secretFilter)
{
    public async Task AddShortMemoryAsync(string source, string content, IReadOnlyList<string> tags, CancellationToken cancellationToken = default)
    {
        var entry = new ShortMemoryEntry(DateTimeOffset.UtcNow, source, secretFilter.Filter(content), tags);
        await jsonlStore.AppendAsync(paths.ShortMemory, entry, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ReadRecentShortMemoryAsync(int count = 10, CancellationToken cancellationToken = default) =>
        jsonlStore.ReadLastLinesAsync(paths.ShortMemory, count, cancellationToken);
}
