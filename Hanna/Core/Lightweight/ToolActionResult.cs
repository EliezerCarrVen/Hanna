namespace Hanna.Core.Lightweight;

public sealed class ToolActionResult
{
    public string RequestId { get; init; } = string.Empty;
    public string Action { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Status { get; init; } = "planned_not_implemented";
    public string Message { get; init; } = "Acción planificada, no implementada.";
    public JsonElement? Data { get; init; }
    public string? ErrorCode { get; init; }
    public DateTimeOffset CompletedUtc { get; init; } = DateTimeOffset.UtcNow;
}
