namespace Hanna.Lightweight.Models;

public sealed record CodeCacheEntry(string Title, string Language, string Summary, string Content, IReadOnlyList<string> Tags, DateTimeOffset CreatedUtc);
