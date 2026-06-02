namespace Hanna.Lightweight.Core;

public sealed class LightweightOptions
{
    public string Mode { get; init; } = "lightweight";
    public string MemoryMode { get; init; } = "flat-file";
    public string DataRoot { get; init; } = Path.Combine(Environment.CurrentDirectory, "HannaData");
    public bool DangerousModulesDryRun { get; init; } = true;
    public int LastEntriesToRead { get; init; } = 10;
    public int MaxJsonlEntriesToRead { get; init; } = 50;
    public int MaxMemoryEntryLength { get; init; } = 4000;
    public int MaxMarkdownNoteLength { get; init; } = 12000;
    public long MaxLogFileBytes { get; init; } = 1_048_576;
    public int MaxSearchResults { get; init; } = 20;
    public long MaxSearchFileBytes { get; init; } = 524_288;
    public int MaxAuditEventsToRead { get; init; } = 30;
    public int MaxCommandLength { get; init; } = 2000;

    public static LightweightOptions CreateDefault() => new();

    public string TruncateMemory(string value) => Truncate(value, MaxMemoryEntryLength);

    public string TruncateMarkdown(string value) => Truncate(value, MaxMarkdownNoteLength);

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
        {
            return value ?? string.Empty;
        }

        return value[..maxLength] + "\n[TRUNCATED_BY_HANNA_LIGHTWEIGHT_LIMIT]";
    }
}
