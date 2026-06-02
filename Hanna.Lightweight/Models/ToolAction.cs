namespace Hanna.Lightweight.Models;

public sealed record ToolAction(string RequestId, string Action, bool RequiresConfirmation, bool DryRun, IReadOnlyDictionary<string, string> Parameters);
