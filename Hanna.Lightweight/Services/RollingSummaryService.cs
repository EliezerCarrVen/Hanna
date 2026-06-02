using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Hanna.Lightweight.Core;

namespace Hanna.Lightweight.Services;

public sealed class RollingSummaryService(
    RuntimePaths paths,
    FlatFileMemoryService memory,
    SecretFilterService secretFilter,
    PathGuardService pathGuard,
    AuditLogService auditLog,
    LightweightOptions options)
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "para", "desde", "esta", "este", "como", "hanna", "lightweight", "prueba", "the", "and", "con", "sin", "una", "uno", "por", "las", "los", "del", "que"
    };

    public async Task<string> RegenerateAsync(CancellationToken cancellationToken = default)
    {
        var entries = await memory.ReadRecentShortMemoryAsync(options.MaxJsonlEntriesToRead, cancellationToken);
        var texts = entries.Select(ExtractContent).Where(static text => !string.IsNullOrWhiteSpace(text)).ToArray();
        var topics = Regex.Matches(string.Join(' ', texts), "[\\p{L}0-9_]{4,}")
            .Cast<Match>()
            .Select(match => match.Value.ToLowerInvariant())
            .Where(word => !StopWords.Contains(word))
            .GroupBy(word => word)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Take(10)
            .Select(group => $"{group.Key} ({group.Count()})")
            .ToArray();

        var securityRedactions = File.Exists(paths.SecurityLog)
            ? File.ReadLines(paths.SecurityLog).Count(line => line.Contains("SecretFilter redacted", StringComparison.OrdinalIgnoreCase))
            : 0;

        var builder = new StringBuilder();
        builder.AppendLine("# Rolling summary local básico");
        builder.AppendLine();
        builder.AppendLine("> Resumen extractivo local, no IA, sin servicios externos.");
        builder.AppendLine();
        builder.AppendLine($"- fecha_utc: {DateTimeOffset.UtcNow:O}");
        builder.AppendLine($"- mensajes_analizados: {entries.Count}");
        builder.AppendLine($"- redacciones_seguridad_detectadas: {securityRedactions}");
        builder.AppendLine();
        builder.AppendLine("## Temas detectados");
        foreach (var topic in topics.DefaultIfEmpty("sin temas suficientes"))
        {
            builder.AppendLine($"- {topic}");
        }
        builder.AppendLine();
        builder.AppendLine("## Últimas acciones");
        foreach (var text in texts.TakeLast(10))
        {
            builder.AppendLine($"- {secretFilter.Filter(options.TruncateMemory(text))}");
        }
        builder.AppendLine();
        builder.AppendLine("## Advertencias de seguridad");
        builder.AppendLine(securityRedactions > 0
            ? $"- Hubo {securityRedactions} redacciones registradas en security.log."
            : "- No hay redacciones registradas hasta ahora.");

        var safePath = pathGuard.EnsureInsideRoot(paths.LastSummary);
        await File.WriteAllTextAsync(safePath, secretFilter.Filter(options.TruncateMarkdown(builder.ToString())), cancellationToken);
        await auditLog.RecordAsync("summary_generated", "Rolling summary local básico regenerado.", true, "info", cancellationToken);
        return safePath;
    }

    private static string ExtractContent(string jsonLine)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonLine);
            if (doc.RootElement.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }
        }
        catch (JsonException)
        {
            return jsonLine;
        }

        return jsonLine;
    }
}
