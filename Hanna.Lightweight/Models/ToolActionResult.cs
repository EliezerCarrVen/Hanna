namespace Hanna.Lightweight.Models;

public sealed record ToolActionResult(string RequestId, string Action, bool Success, string Status, string Message);
