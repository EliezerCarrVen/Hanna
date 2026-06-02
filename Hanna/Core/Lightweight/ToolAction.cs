namespace Hanna.Core.Lightweight;

public sealed class ToolAction
{
    public string Action { get; init; } = string.Empty;
    public string RequestId { get; init; } = Guid.NewGuid().ToString("N");
    public JsonElement Parameters { get; init; }
    public bool RequiresConfirmation { get; init; }
    public bool DryRun { get; init; } = true;
    public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
