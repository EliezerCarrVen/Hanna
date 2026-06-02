namespace Hanna.Core.Lightweight;

public sealed class ShortMemoryEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
    public string Source { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public IReadOnlyList<string> Tags { get; init; } = [];
    public int Importance { get; init; }
    public DateTimeOffset? ExpiresUtc { get; init; }
    public string Sensitivity { get; init; } = "normal";
}
