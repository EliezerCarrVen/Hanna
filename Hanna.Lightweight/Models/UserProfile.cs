namespace Hanna.Lightweight.Models;

public sealed record UserProfile(string UserId, string DisplayName, IReadOnlyList<string> Roles, bool IsLocalOnly);
