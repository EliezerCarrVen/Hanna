using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class VaultIndexService(RuntimePaths paths, PathGuardService pathGuard, SecretFilterService secretFilter, JsonlStoreService jsonlStore, AuditLogService auditLog, LightweightOptions options)
{
    public async Task<int> RebuildAsync(CancellationToken cancellationToken = default)
    {
        var vault = pathGuard.EnsureInsideRoot(paths.Vault);
        var index = pathGuard.EnsureInsideRoot(paths.VaultIndex);
        Directory.CreateDirectory(Path.GetDirectoryName(index) ?? paths.Indexes);
        await File.WriteAllTextAsync(index, string.Empty, cancellationToken);

        if (!Directory.Exists(vault))
        {
            return 0;
        }

        var count = 0;
        foreach (var file in Directory.EnumerateFiles(vault, "*.*", SearchOption.AllDirectories))
        {
            var safeFile = pathGuard.EnsureInsideRoot(file);
            var info = new FileInfo(safeFile);
            if (info.Length > options.MaxSearchFileBytes)
            {
                continue;
            }

            var content = await File.ReadAllTextAsync(safeFile, cancellationToken);
            var tags = ExtractTags(content).Select(secretFilter.Filter).ToArray();
            await jsonlStore.AppendAsync(index, new
            {
                relativePath = Path.GetRelativePath(paths.DataRoot, safeFile),
                name = secretFilter.Filter(info.Name),
                extension = info.Extension,
                size = info.Length,
                modifiedUtc = info.LastWriteTimeUtc,
                sha256 = ComputeSha256(content),
                tags
            }, cancellationToken);
            count++;
        }

        await auditLog.RecordAsync("vault_index_rebuilt", $"Vault index rebuilt with {count} file(s).", true, "info", cancellationToken);
        return count;
    }

    public string GetStatus()
    {
        var index = pathGuard.EnsureInsideRoot(paths.VaultIndex);
        var count = File.Exists(index) ? File.ReadLines(index).Count(line => !string.IsNullOrWhiteSpace(line)) : 0;
        var size = File.Exists(index) ? new FileInfo(index).Length : 0;
        return $"vault_index: entries={count}, size_bytes={size}, path={index}";
    }

    private static IReadOnlyList<string> ExtractTags(string content)
    {
        if (!content.StartsWith("---", StringComparison.Ordinal))
        {
            return [];
        }

        var end = content.IndexOf("\n---", 3, StringComparison.Ordinal);
        if (end < 0)
        {
            return [];
        }

        var frontMatter = content[..end];
        var tagsLine = frontMatter.Split('\n').FirstOrDefault(line => line.TrimStart().StartsWith("tags:", StringComparison.OrdinalIgnoreCase));
        if (tagsLine is null)
        {
            return [];
        }

        return Regex.Matches(tagsLine, "[\\p{L}0-9_:-]+")
            .Cast<Match>()
            .Select(match => match.Value)
            .Where(tag => !tag.Equals("tags", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static string ComputeSha256(string content) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(content))).ToLowerInvariant();
}
