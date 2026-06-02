using Hanna.Lightweight.Core;
using Hanna.Lightweight.Models;

namespace Hanna.Lightweight.Services;

public sealed class AuditLogService(RuntimePaths paths, JsonlStoreService jsonlStore, LightweightOptions options)
{
    public Task RecordAsync(string eventType, string description, bool dryRun = true, string severity = "info", CancellationToken cancellationToken = default)
    {
        var auditEvent = new AuditEvent(DateTimeOffset.UtcNow, eventType, "local-console", description, dryRun, severity);
        return jsonlStore.AppendAsync(paths.AuditLog, auditEvent, cancellationToken);
    }

    public Task RecordCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        var safeCommand = command.Length > options.MaxCommandLength ? command[..options.MaxCommandLength] + " [TRUNCATED]" : command;
        return RecordAsync("command_executed", safeCommand, true, "info", cancellationToken);
    }

    public Task<IReadOnlyList<string>> ReadRecentAsync(int? count = null, CancellationToken cancellationToken = default) =>
        jsonlStore.ReadLastLinesAsync(paths.AuditLog, Math.Min(count ?? options.MaxAuditEventsToRead, options.MaxAuditEventsToRead), cancellationToken);
}
