namespace Hanna.Lightweight.Models;

public sealed record ShortMemoryEntry(DateTimeOffset TimestampUtc, string Source, string Content, IReadOnlyList<string> Tags);
