using Hanna.Lightweight.Core;
using Hanna.Lightweight.Models;

namespace Hanna.Lightweight.Services;

public sealed class AuditLogService(RuntimePaths paths, JsonlStoreService jsonlStore)
{
    public Task RecordAsync(string eventType, string description, bool dryRun = true, string severity = "info", CancellationToken cancellationToken = default)
    {
        var auditEvent = new AuditEvent(DateTimeOffset.UtcNow, eventType, "local-console", description, dryRun, severity);
        return jsonlStore.AppendAsync(paths.AuditLog, auditEvent, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ReadRecentAsync(int count = 10, CancellationToken cancellationToken = default) =>
        jsonlStore.ReadLastLinesAsync(paths.AuditLog, count, cancellationToken);
}
