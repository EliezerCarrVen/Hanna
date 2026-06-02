namespace Hanna.Lightweight.Core;

public sealed class LightweightOptions
{
    public string Mode { get; init; } = "lightweight";
    public string MemoryMode { get; init; } = "flat-file";
    public string DataRoot { get; init; } = Path.Combine(Environment.CurrentDirectory, "HannaData");
    public bool DangerousModulesDryRun { get; init; } = true;
    public int LastEntriesToRead { get; init; } = 10;

    public static LightweightOptions CreateDefault() => new();
}
