namespace Hanna.Lightweight.Models;

public sealed record MarkdownMemoryNote(string Title, string Category, string Body, DateTimeOffset CreatedUtc, IReadOnlyList<string> Tags);
