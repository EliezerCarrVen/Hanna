namespace Hanna.Core.Lightweight;

public sealed class FlatFileMemoryOptions
{
    public bool Enabled { get; init; }
    public string VaultPath { get; init; } = string.Empty;
    public string ShortMemoryPath { get; init; } = string.Empty;
    public string CurrentSessionPath { get; init; } = string.Empty;
    public string RollingSummaryPath { get; init; } = string.Empty;
    public string RipgrepPath { get; init; } = "rg";
    public bool RollingSummaryEnabled { get; init; }
    public int MaxShortMemoryEntries { get; init; } = 500;
    public int MaxEntryCharacters { get; init; } = 4_000;
    public IReadOnlyList<string> AllowedVaultSubdirectories { get; init; } =
        ["personas", "proyectos", "memoria", "inventario", "tareas", "sistema"];
}
