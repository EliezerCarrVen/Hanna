using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hanna.Lightweight.Core;
using Hanna.Lightweight.Models;

namespace Hanna.Lightweight.Services;

public sealed class CodeCacheService(
    RuntimePaths paths,
    MarkdownVaultService markdownVault,
    JsonlStoreService jsonlStore,
    SecretFilterService secretFilter,
    PathGuardService pathGuard,
    LightweightOptions options)
{
    public async Task<string> CreateTestCodeCacheAsync(CancellationToken cancellationToken = default)
    {
        var entry = new CodeCacheEntry(
            "Cache de código JWT seguro",
            "csharp",
            "Entrada mínima para validar búsqueda de código sin guardar secretos ni claves JWT.",
            "// jwt validation placeholder; no token, no secret, no password is persisted here.",
            ["lenguaje:csharp", "tema:jwt", "origen:self-test", "codigo_cache", "jwt", "prueba"],
            DateTimeOffset.UtcNow);

        return await CreateCodeCacheAsync(entry, cancellationToken);
    }

    public async Task<string> CreateCodeCacheAsync(CodeCacheEntry entry, CancellationToken cancellationToken = default)
    {
        var safeContent = secretFilter.Filter(options.TruncateMarkdown(entry.Content));
        var hash = ComputeSha256($"{entry.Language}\n{entry.Summary}\n{safeContent}");
        var existing = FindByHash(hash);
        if (existing is not null)
        {
            return Path.Combine(paths.DataRoot, existing);
        }

        var body = $"## Resumen\n{secretFilter.Filter(entry.Summary)}\n\n## Metadatos\n- lenguaje: {secretFilter.Filter(entry.Language)}\n- tema: jwt\n- origen: hanna-lightweight\n- fecha: {entry.CreatedUtc:O}\n- hash_sha256: {hash}\n\n```{secretFilter.Filter(entry.Language)}\n{safeContent}\n```";
        var note = new MarkdownMemoryNote(entry.Title, "codigo_cache", body, entry.CreatedUtc, entry.Tags);
        var path = await markdownVault.CreateNoteAsync(paths.VaultCodigoCache, note, cancellationToken);
        await jsonlStore.AppendAsync(paths.CodeCacheIndex, new
        {
            hash,
            path = Path.GetRelativePath(paths.DataRoot, path),
            title = secretFilter.Filter(entry.Title),
            language = secretFilter.Filter(entry.Language),
            tags = entry.Tags.Select(secretFilter.Filter).ToArray(),
            createdUtc = entry.CreatedUtc
        }, cancellationToken);
        return path;
    }

    public IReadOnlyList<string> ListEntries(int count = 20)
    {
        var safePath = pathGuard.EnsureInsideRoot(paths.CodeCacheIndex);
        if (!File.Exists(safePath))
        {
            return [];
        }

        return File.ReadLines(safePath)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .TakeLast(Math.Min(count, options.MaxJsonlEntriesToRead))
            .ToArray();
    }

    public string GetStatus()
    {
        var noteCount = Directory.Exists(paths.VaultCodigoCache) ? Directory.EnumerateFiles(paths.VaultCodigoCache, "*.md", SearchOption.AllDirectories).Count() : 0;
        var indexCount = File.Exists(paths.CodeCacheIndex) ? File.ReadLines(paths.CodeCacheIndex).Count(line => !string.IsNullOrWhiteSpace(line)) : 0;
        return $"code cache: notes={noteCount}, indexed_entries={indexCount}, dedupe=sha256, translation=planned_not_implemented";
    }

    private string? FindByHash(string hash)
    {
        var safePath = pathGuard.EnsureInsideRoot(paths.CodeCacheIndex);
        if (!File.Exists(safePath))
        {
            return null;
        }

        foreach (var line in File.ReadLines(safePath))
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.TryGetProperty("hash", out var hashProperty)
                    && string.Equals(hashProperty.GetString(), hash, StringComparison.OrdinalIgnoreCase)
                    && doc.RootElement.TryGetProperty("path", out var pathProperty))
                {
                    var relativePath = pathProperty.GetString();
                    if (!string.IsNullOrWhiteSpace(relativePath))
                    {
                        return relativePath;
                    }
                }
            }
            catch (JsonException)
            {
                continue;
            }
        }

        return null;
    }

    private static string ComputeSha256(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
