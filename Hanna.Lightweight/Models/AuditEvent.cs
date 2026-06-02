namespace Hanna.Lightweight.Models;

public sealed record AuditEvent(DateTimeOffset TimestampUtc, string EventType, string Actor, string Description, bool DryRun, string Severity);
