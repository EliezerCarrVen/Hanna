using System.Text;
using Hanna.Lightweight.Core;
using Hanna.Lightweight.Models;

namespace Hanna.Lightweight.Services;

public sealed class MarkdownVaultService(RuntimePaths paths, SecretFilterService secretFilter, LightweightOptions options, PathGuardService pathGuard)
{
    public Task<string> CreateMemoryNoteAsync(string title, string body, CancellationToken cancellationToken = default) =>
        CreateNoteAsync(paths.VaultMemoria, new MarkdownMemoryNote(title, "memoria", body, DateTimeOffset.UtcNow, ["prueba", "flat-file"]), cancellationToken);

    public async Task<string> CreateNoteAsync(string directory, MarkdownMemoryNote note, CancellationToken cancellationToken = default)
    {
        var safeDirectory = pathGuard.EnsureInsideRoot(directory);
        Directory.CreateDirectory(safeDirectory);
        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-ffff}-{Slug(note.Title)}.md";
        var path = pathGuard.EnsureInsideRoot(Path.Combine(safeDirectory, fileName));
        var builder = new StringBuilder();
        builder.AppendLine("---");
        builder.AppendLine($"type: {secretFilter.Filter(note.Category)}");
        builder.AppendLine($"title: {secretFilter.Filter(note.Title)}");
        builder.AppendLine($"created_utc: {note.CreatedUtc:O}");
        builder.AppendLine($"tags: [{string.Join(", ", note.Tags.Select(secretFilter.Filter))}]");
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine(secretFilter.Filter(options.TruncateMarkdown(note.Body)));
        await File.WriteAllTextAsync(path, options.TruncateMarkdown(builder.ToString()), cancellationToken);
        return path;
    }

    public int CountMarkdownNotes(string? directory = null)
    {
        var root = pathGuard.EnsureInsideRoot(directory ?? paths.Vault);
        return Directory.Exists(root) ? Directory.EnumerateFiles(root, "*.md", SearchOption.AllDirectories).Count() : 0;
    }

    private static string Slug(string value)
    {
        var chars = value.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        var slug = string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries)).Trim('-');
        return string.IsNullOrWhiteSpace(slug) ? "nota" : slug;
    }
}
