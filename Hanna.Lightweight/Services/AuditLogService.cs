using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hanna.Lightweight.Core;
using Hanna.Lightweight.Models;

namespace Hanna.Lightweight.Services;

public sealed class AuditLogService(RuntimePaths paths, JsonlStoreService jsonlStore, LightweightOptions options, PathGuardService pathGuard)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task RecordAsync(string eventType, string description, bool dryRun = true, string severity = "info", CancellationToken cancellationToken = default)
    {
        await RecordDetailedAsync(eventType, module: eventType, command: null, result: severity, description, dryRun, redacted: description.Contains("[REDACTED]", StringComparison.Ordinal), cancellationToken);
    }

    public Task RecordCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        var safeCommand = command.Length > options.MaxCommandLength ? command[..options.MaxCommandLength] + " [TRUNCATED]" : command;
        return RecordDetailedAsync("command_executed", "console", safeCommand, "accepted", safeCommand, true, safeCommand.Contains("[REDACTED]", StringComparison.Ordinal), cancellationToken);
    }

    public async Task RecordDetailedAsync(string eventType, string module, string? command, string result, string description, bool dryRun, bool redacted = false, CancellationToken cancellationToken = default)
    {
        var previousHash = GetLastHash();
        var payload = new SortedDictionary<string, object?>
        {
            ["event_id"] = Guid.NewGuid().ToString("N"),
            ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
            ["actor"] = "local-root",
            ["eventType"] = eventType,
            ["command"] = command,
            ["module"] = module,
            ["result"] = result,
            ["description"] = description,
            ["dry_run"] = dryRun,
            ["redacted"] = redacted,
            ["previous_hash"] = previousHash
        };
        var canonical = JsonSerializer.Serialize(payload, JsonOptions);
        payload["current_hash"] = ComputeHash(canonical);
        await jsonlStore.AppendAsync(paths.AuditLog, payload, cancellationToken);
    }

    public Task<IReadOnlyList<string>> ReadRecentAsync(int? count = null, CancellationToken cancellationToken = default) =>
        jsonlStore.ReadLastLinesAsync(paths.AuditLog, Math.Min(count ?? options.MaxAuditEventsToRead, options.MaxAuditEventsToRead), cancellationToken);

    public (bool ok, string message, int entries) VerifyHashChain()
    {
        var safePath = pathGuard.EnsureInsideRoot(paths.AuditLog);
        if (!File.Exists(safePath))
        {
            return (true, "audit.log no existe todavía", 0);
        }

        string previous = "GENESIS";
        var count = 0;
        foreach (var line in File.ReadLines(safePath).Where(static l => !string.IsNullOrWhiteSpace(l)))
        {
            count++;
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;
            if (!root.TryGetProperty("current_hash", out var currentProperty) || !root.TryGetProperty("previous_hash", out var previousProperty))
            {
                return (false, $"entrada legacy sin hash-chain en posición {count}", count);
            }

            var current = currentProperty.GetString() ?? string.Empty;
            var storedPrevious = previousProperty.GetString() ?? string.Empty;
            if (!string.Equals(storedPrevious, previous, StringComparison.Ordinal))
            {
                return (false, $"previous_hash inválido en entrada {count}", count);
            }

            var payload = new SortedDictionary<string, object?>();
            foreach (var property in root.EnumerateObject())
            {
                if (property.NameEquals("current_hash"))
                {
                    continue;
                }

                payload[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    JsonValueKind.Number when property.Value.TryGetInt64(out var n) => n,
                    _ => property.Value.GetRawText().Trim('"')
                };
            }

            var recomputed = ComputeHash(JsonSerializer.Serialize(payload, JsonOptions));
            if (!string.Equals(recomputed, current, StringComparison.OrdinalIgnoreCase))
            {
                return (false, $"current_hash inválido en entrada {count}", count);
            }

            previous = current;
        }

        return (true, $"hash-chain válida con {count} eventos", count);
    }

    public string ExportAudit()
    {
        pathGuard.EnsureInsideRoot(paths.AuditLog);
        return File.Exists(paths.AuditLog) ? File.ReadAllText(paths.AuditLog) : string.Empty;
    }

    private string GetLastHash()
    {
        var safePath = pathGuard.EnsureInsideRoot(paths.AuditLog);
        if (!File.Exists(safePath))
        {
            return "GENESIS";
        }

        var last = File.ReadLines(safePath).LastOrDefault(static line => !string.IsNullOrWhiteSpace(line));
        if (string.IsNullOrWhiteSpace(last))
        {
            return "GENESIS";
        }

        try
        {
            using var doc = JsonDocument.Parse(last);
            return doc.RootElement.TryGetProperty("current_hash", out var hash) ? hash.GetString() ?? "GENESIS" : "GENESIS";
        }
        catch (JsonException)
        {
            return "BROKEN_CHAIN";
        }
    }

    private static string ComputeHash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
