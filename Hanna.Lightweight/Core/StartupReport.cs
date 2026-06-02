namespace Hanna.Lightweight.Core;

public sealed record StartupReport(
    string Mode,
    string MemoryMode,
    string VaultPath,
    string ShortMemoryPath,
    bool RipgrepAvailable,
    IReadOnlyList<ModuleStatus> Modules);
