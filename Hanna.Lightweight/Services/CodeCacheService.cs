using Hanna.Lightweight.Core;
using Hanna.Lightweight.Models;

namespace Hanna.Lightweight.Services;

public sealed class CodeCacheService(RuntimePaths paths, MarkdownVaultService markdownVault, JsonlStoreService jsonlStore, SecretFilterService secretFilter)
{
    public async Task<string> CreateTestCodeCacheAsync(CancellationToken cancellationToken = default)
    {
        var entry = new CodeCacheEntry(
            "Cache de código JWT seguro",
            "csharp",
            "Entrada mínima para validar búsqueda de código sin guardar secretos ni claves JWT.",
            "// jwt validation placeholder; no token, no secret, no password is persisted here.",
            ["codigo_cache", "jwt", "prueba"],
            DateTimeOffset.UtcNow);

        var note = new MarkdownMemoryNote(entry.Title, "codigo_cache", $"## Resumen\n{entry.Summary}\n\n```{entry.Language}\n{entry.Content}\n```", entry.CreatedUtc, entry.Tags);
        var path = await markdownVault.CreateNoteAsync(paths.VaultCodigoCache, note, cancellationToken);
        await jsonlStore.AppendAsync(paths.CodeCacheIndex, new
        {
            path = Path.GetRelativePath(paths.DataRoot, path),
            title = secretFilter.Filter(entry.Title),
            tags = entry.Tags,
            createdUtc = entry.CreatedUtc
        }, cancellationToken);
        return path;
    }
}
