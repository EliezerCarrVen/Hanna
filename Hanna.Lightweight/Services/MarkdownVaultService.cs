using System.Text;
using Hanna.Lightweight.Core;
using Hanna.Lightweight.Models;

namespace Hanna.Lightweight.Services;

public sealed class MarkdownVaultService(RuntimePaths paths, SecretFilterService secretFilter)
{
    public Task<string> CreateMemoryNoteAsync(string title, string body, CancellationToken cancellationToken = default) =>
        CreateNoteAsync(paths.VaultMemoria, new MarkdownMemoryNote(title, "memoria", body, DateTimeOffset.UtcNow, ["prueba", "flat-file"]), cancellationToken);

    public async Task<string> CreateNoteAsync(string directory, MarkdownMemoryNote note, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(directory);
        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{Slug(note.Title)}.md";
        var path = Path.Combine(directory, fileName);
        var builder = new StringBuilder();
        builder.AppendLine("---");
        builder.AppendLine($"type: {secretFilter.Filter(note.Category)}");
        builder.AppendLine($"title: {secretFilter.Filter(note.Title)}");
        builder.AppendLine($"created_utc: {note.CreatedUtc:O}");
        builder.AppendLine($"tags: [{string.Join(", ", note.Tags.Select(secretFilter.Filter))}]");
        builder.AppendLine("---");
        builder.AppendLine();
        builder.AppendLine(secretFilter.Filter(note.Body));
        await File.WriteAllTextAsync(path, builder.ToString(), cancellationToken);
        return path;
    }

    private static string Slug(string value)
    {
        var chars = value.ToLowerInvariant().Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        return string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries)).Trim('-');
    }
}
