using Hanna.Core;
using Microsoft.Data.Sqlite;

namespace Hanna.Services;

internal sealed class TieredMemoryService
{
    private readonly AppConfig config;
    private readonly object sync = new();

    public TieredMemoryService(AppConfig config)
    {
        this.config = config;
        // V6.4: no se importan índices JSONL durante el arranque.
        // En equipos lentos esto podía hacer que Hanna pareciera quedarse atorada después de MongoDB.
        // La base se crea rápido y las importaciones/compresión quedan para mantenimiento nocturno o búsqueda bajo demanda.
        EnsureDatabase();
        Console.WriteLine("[TieredMemory] Memoria jerárquica lista sin bloqueo de arranque.");
    }

    public void EnsureDatabase()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(config.TieredMemoryDbPath) ?? config.TieredMemoryRoot);
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = @"
CREATE TABLE IF NOT EXISTS memory_index (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    date TEXT NOT NULL,
    type TEXT NOT NULL DEFAULT 'daily_summary',
    summary TEXT NOT NULL,
    tags TEXT DEFAULT '',
    location TEXT NOT NULL DEFAULT 'LOCAL',
    archive_path TEXT DEFAULT '',
    hash TEXT DEFAULT '',
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS idx_memory_index_date ON memory_index(date);
CREATE INDEX IF NOT EXISTS idx_memory_index_location ON memory_index(location);
CREATE INDEX IF NOT EXISTS idx_memory_index_hash ON memory_index(hash);
CREATE UNIQUE INDEX IF NOT EXISTS idx_memory_index_hash_unique ON memory_index(hash) WHERE hash IS NOT NULL AND hash <> '';
";
            command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[TieredMemory] No se pudo inicializar index.db: " + ex.Message);
        }
    }

    public async Task UpsertDailySummaryAsync(string date, string summary, string tags, string location, string archivePath, string hash, CancellationToken cancellationToken)
    {
        date = string.IsNullOrWhiteSpace(date) ? DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : date.Trim();
        summary = (summary ?? "").Trim();
        if (string.IsNullOrWhiteSpace(summary))
            summary = "Sin actividad relevante detectada.";

        if (IsInternalOrUnsafeSummary(summary, tags, archivePath))
            return;

        string now = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture);

        lock (sync)
        {
            EnsureDatabase();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            bool hasHash = !string.IsNullOrWhiteSpace(hash);

            if (hasHash)
            {
                command.CommandText = @"
UPDATE memory_index
SET summary = $summary, tags = $tags, location = $location, archive_path = $archive, updated_at = $now
WHERE hash = $hash;

INSERT INTO memory_index(date, type, summary, tags, location, archive_path, hash, created_at, updated_at)
SELECT $date, 'daily_summary', $summary, $tags, $location, $archive, $hash, $now, $now
WHERE changes() = 0;";
            }
            else
            {
                command.CommandText = @"
INSERT INTO memory_index(date, type, summary, tags, location, archive_path, hash, created_at, updated_at)
SELECT $date, 'daily_summary', $summary, $tags, $location, $archive, '', $now, $now
WHERE NOT EXISTS (SELECT 1 FROM memory_index WHERE date = $date AND summary = $summary LIMIT 1);";
            }

            command.Parameters.AddWithValue("$date", date);
            command.Parameters.AddWithValue("$summary", summary);
            command.Parameters.AddWithValue("$tags", tags ?? "");
            command.Parameters.AddWithValue("$location", string.IsNullOrWhiteSpace(location) ? "LOCAL" : location);
            command.Parameters.AddWithValue("$archive", archivePath ?? "");
            command.Parameters.AddWithValue("$hash", hash ?? "");
            command.Parameters.AddWithValue("$now", now);
            command.ExecuteNonQuery();
        }

        await AppendJsonlMirror(date, summary, tags, location, archivePath, hash, cancellationToken);
    }

    public IReadOnlyList<TieredMemoryHit> Search(string query, int limit = 5)
    {
        var results = new List<TieredMemoryHit>();
        string normalizedQuery = Normalize(query);
        if (string.IsNullOrWhiteSpace(normalizedQuery))
            return results;

        results.AddRange(SearchSqlite(normalizedQuery, limit));
        results.AddRange(SearchJsonl(normalizedQuery, limit));

        return results
            .Where(x => !IsInternalOrUnsafeSummary(x.Summary, x.Tags, x.Archive))
            .GroupBy(x => NormalizeForDedupe(x.Summary))
            .Select(g => g.OrderByDescending(x => x.Score).First())
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Date)
            .Take(Math.Clamp(limit, 1, 10))
            .ToList();
    }

    public string BuildContext(string query, int limit = 3)
    {
        var hits = Search(query, limit);
        if (hits.Count == 0)
            return "";

        var sb = new StringBuilder();
        sb.AppendLine("Contexto local recuperado:");
        foreach (var hit in hits)
        {
            sb.Append("- ");
            if (!string.IsNullOrWhiteSpace(hit.Date)) sb.Append(hit.Date).Append(": ");
            sb.Append(SanitizeVisibleSummary(hit.Summary));
            if (!string.IsNullOrWhiteSpace(hit.Tags)) sb.Append(" [").Append(hit.Tags).Append(']');
            if (!string.IsNullOrWhiteSpace(hit.Location)) sb.Append(" (ubicación: ").Append(hit.Location).Append(')');
            if (hit.Location.Contains("REMOTE", StringComparison.OrdinalIgnoreCase))
                sb.Append(". Si se necesita el detalle completo, requiere recuperar el archivo remoto.");
            sb.AppendLine();
        }
        return sb.ToString().Trim();
    }

    public string FormatSearchResult(string query, int limit = 5)
    {
        var hits = Search(query, limit);
        if (hits.Count == 0)
            return "No encontré coincidencias en la memoria local/index.db de Hanna.";

        var sb = new StringBuilder();
        sb.AppendLine("Encontré esto en la memoria local de Hanna:");
        int i = 1;
        foreach (var hit in hits)
        {
            sb.Append(i++).Append(". ");
            if (!string.IsNullOrWhiteSpace(hit.Date)) sb.Append(hit.Date).Append(" — ");
            sb.Append(SanitizeVisibleSummary(hit.Summary));
            if (!string.IsNullOrWhiteSpace(hit.Tags)) sb.Append("\n   Etiquetas: ").Append(hit.Tags);
            if (!string.IsNullOrWhiteSpace(hit.Location)) sb.Append("\n   Ubicación: ").Append(hit.Location);
            if (!string.IsNullOrWhiteSpace(hit.Archive)) sb.Append("\n   Archivo: ").Append(hit.Archive);
            sb.AppendLine();
        }
        return sb.ToString().Trim();
    }

    private IEnumerable<TieredMemoryHit> SearchSqlite(string normalizedQuery, int limit)
    {
        var results = new List<TieredMemoryHit>();
        try
        {
            EnsureDatabase();
            using var connection = OpenConnection();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT date, summary, tags, location, archive_path FROM memory_index ORDER BY date DESC, id DESC LIMIT 1000";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                string date = reader.GetString(0);
                string summary = reader.GetString(1);
                string tags = reader.IsDBNull(2) ? "" : reader.GetString(2);
                string location = reader.IsDBNull(3) ? "LOCAL" : reader.GetString(3);
                string archive = reader.IsDBNull(4) ? "" : reader.GetString(4);
                string haystack = Normalize(date + " " + summary + " " + tags + " " + archive);
                int score = Score(normalizedQuery, haystack);
                if (score > 0 && !IsInternalOrUnsafeSummary(summary, tags, archive))
                    results.Add(new TieredMemoryHit(date, summary, tags, location, archive, score));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[TieredMemory] Falló búsqueda SQLite: " + ex.Message);
        }
        return results.OrderByDescending(x => x.Score).Take(Math.Clamp(limit, 1, 10));
    }

    private IEnumerable<TieredMemoryHit> SearchJsonl(string normalizedQuery, int limit)
    {
        var results = new List<TieredMemoryHit>();
        foreach (string indexPath in CandidateJsonlIndexes())
        {
            if (!File.Exists(indexPath))
                continue;

            foreach (string line in SafeReadLines(indexPath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    using JsonDocument doc = JsonDocument.Parse(line);
                    TieredMemoryHit hit = ParseJsonlHit(doc.RootElement, normalizedQuery);
                    if (hit.Score > 0)
                        results.Add(hit);
                }
                catch { }
            }
        }
        return results.OrderByDescending(x => x.Score).Take(Math.Clamp(limit, 1, 10));
    }

    private void ImportJsonlIndexesToSqlite()
    {
        try
        {
            foreach (string indexPath in CandidateJsonlIndexes())
            {
                if (!File.Exists(indexPath))
                    continue;
                foreach (string line in SafeReadLines(indexPath))
                {
                    if (string.IsNullOrWhiteSpace(line))
                        continue;
                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(line);
                        JsonElement root = doc.RootElement;
                        string date = ReadString(root, "date", "fecha", "day", "dia");
                        string summary = ReadString(root, "summary", "resumen", "resumenEjecutivo", "Resumen_Ejecutivo");
                        string tags = ReadString(root, "tags", "etiquetas");
                        string location = ReadString(root, "location", "ubicacion", "ubicación", "estado");
                        string archive = ReadString(root, "archive", "archivo", "path", "ruta", "file");
                        string hash = ReadString(root, "hash", "sha256");
                        if (!string.IsNullOrWhiteSpace(summary))
                            UpsertDailySummaryAsync(date, summary, tags, location, archive, hash, CancellationToken.None).GetAwaiter().GetResult();
                    }
                    catch { }
                }
            }
        }
        catch { }
    }

    private TieredMemoryHit ParseJsonlHit(JsonElement root, string normalizedQuery)
    {
        string date = ReadString(root, "date", "fecha", "day", "dia");
        string summary = ReadString(root, "summary", "resumen", "resumenEjecutivo", "Resumen_Ejecutivo");
        string tags = ReadString(root, "tags", "etiquetas");
        string location = ReadString(root, "location", "ubicacion", "ubicación", "estado");
        string archive = ReadString(root, "archive", "archivo", "path", "ruta", "file");
        string haystack = Normalize(date + " " + summary + " " + tags + " " + archive);
        int score = Score(normalizedQuery, haystack);
        if (IsInternalOrUnsafeSummary(summary, tags, archive))
            score = 0;
        return new TieredMemoryHit(date, summary, tags, string.IsNullOrWhiteSpace(location) ? "LOCAL" : location, archive, score);
    }

    private static bool IsInternalOrUnsafeSummary(string summary, string? tags = null, string? archive = null)
    {
        string value = Normalize((summary ?? "") + " " + (tags ?? "") + " " + (archive ?? ""));

        if (string.IsNullOrWhiteSpace(value))
            return true;

        string[] blocked =
        {
            "contexto modular de hanna",
            "hanna debe comportarse",
            "si el usuario dice hanna para",
            "siempre debes decir la verdad",
            "paso final de seguridad",
            "spotify playlists",
            "modismos mexicanos",
            "hannaenv",
            "telegram token",
            "api key",
            "jwt secret",
            "pairing token",
            "personalidad txt",
            "reglas verdad",
            "prompt",
            "system prompt",
            "instrucciones internas"
        };

        return blocked.Any(value.Contains);
    }

    private static string SanitizeVisibleSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return "Resumen local sin contenido visible.";

        string cleaned = summary.Replace("===", " ");
        cleaned = Regex.Replace(cleaned, @"---\s*[^\r\n]+\s*---", " ");
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();

        if (cleaned.Length > 500)
            cleaned = cleaned[..500].Trim() + "...";

        return cleaned;
    }

    private static string NormalizeForDedupe(string value)
    {
        value = Normalize(value);
        if (value.Length > 280)
            value = value[..280];
        return value;
    }

    private async Task AppendJsonlMirror(string date, string summary, string tags, string location, string archivePath, string hash, CancellationToken cancellationToken)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(config.TieredMemoryIndexPath) ?? config.TieredMemoryRoot);
            var obj = new
            {
                date,
                type = "daily_summary",
                summary = summary.Length > 1200 ? summary[..1200] : summary,
                tags = (tags ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                location = string.IsNullOrWhiteSpace(location) ? "LOCAL" : location,
                file = archivePath ?? "",
                hash = hash ?? "",
                createdAt = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture)
            };
            await File.AppendAllTextAsync(config.TieredMemoryIndexPath, JsonSerializer.Serialize(obj) + Environment.NewLine, Encoding.UTF8, cancellationToken);
        }
        catch { }
    }

    private SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection($"Data Source={config.TieredMemoryDbPath}");
        connection.Open();
        return connection;
    }

    private IEnumerable<string> CandidateJsonlIndexes()
    {
        yield return config.TieredMemoryIndexPath;
        yield return Path.Combine(config.BaseDirectory, "extras", "hanna_v6", "Maintenance", "data", "hanna_memory", "index", "index.jsonl");
        yield return Path.Combine(config.TieredMemoryRoot, "index.jsonl");
    }

    private static IEnumerable<string> SafeReadLines(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        while (!reader.EndOfStream)
            yield return reader.ReadLine() ?? "";
    }

    private static int Score(string query, string haystack)
    {
        int score = 0;
        foreach (string token in query.Split(' ', StringSplitOptions.RemoveEmptyEntries).Distinct())
        {
            if (token.Length < 3) continue;
            if (haystack.Contains(token)) score += token.Length >= 6 ? 3 : 1;
        }
        return score;
    }

    private static string Normalize(string value)
    {
        value = (value ?? "").ToLowerInvariant();
        value = Regex.Replace(value, @"[^a-z0-9áéíóúñü\s]", " ");
        value = Regex.Replace(value, @"\s+", " ").Trim();
        return value;
    }

    private static string ReadString(JsonElement element, params string[] names)
    {
        foreach (string name in names)
        {
            if (element.TryGetProperty(name, out JsonElement value))
            {
                if (value.ValueKind == JsonValueKind.Array)
                    return string.Join(", ", value.EnumerateArray().Select(x => x.ToString()));
                return value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : value.ToString();
            }
        }
        return "";
    }
}

internal sealed record TieredMemoryHit(string Date, string Summary, string Tags, string Location, string Archive, int Score);
