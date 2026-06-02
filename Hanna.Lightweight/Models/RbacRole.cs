namespace Hanna.Lightweight.Models;

public sealed record RbacRole(string Name, IReadOnlyList<string> Permissions, bool PlannedOnly = true);
