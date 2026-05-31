using Hanna.Core;

namespace Hanna.Services;

internal sealed class RoutineService
{
    private readonly FileStorageService storage;

    public RoutineService(FileStorageService storage)
    {
        this.storage = storage;
    }

    public async Task<string> CreateRoutine(long chatId, string originalText, CancellationToken cancellationToken)
    {
        string name = ExtractRoutineName(originalText);

        if (string.IsNullOrWhiteSpace(name))
            return "Dime cómo se llamará la rutina. Por ejemplo: crea rutina estudio.";

        var routines = await Load(chatId, cancellationToken);
        routines[name] = originalText.Trim();

        await Save(chatId, routines, cancellationToken);

        return $"Listo. Guardé la rutina {name}. Por ahora la dejo como rutina personalizada registrada.";
    }

    public async Task<string> ListRoutines(long chatId, CancellationToken cancellationToken)
    {
        var routines = await Load(chatId, cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("Rutinas disponibles:");
        sb.AppendLine("- estudio");
        sb.AppendLine("- musica");
        sb.AppendLine("- noche");

        foreach (var item in routines.Keys.Where(x => x is not "estudio" and not "musica" and not "noche"))
            sb.AppendLine("- " + item);

        return sb.ToString().Trim();
    }

    public string DetectRoutineName(string originalText)
    {
        string normalized = Utilities.TextTools.Normalize(originalText);

        if (normalized.Contains("buenas noches") || normalized.Contains("modo noche"))
            return "noche";

        if (normalized.Contains("modo estudio") || normalized.Contains("estudio") || normalized.Contains("concentracion") || normalized.Contains("concentración"))
            return "estudio";

        if (normalized.Contains("modo musica") || normalized.Contains("modo música") || normalized.Contains("musica") || normalized.Contains("música"))
            return "musica";

        return ExtractRoutineName(originalText);
    }

    public async Task<Dictionary<string, string>> Load(long chatId, CancellationToken cancellationToken)
    {
        string path = storage.GetRoutinesPath(chatId);

        if (!File.Exists(path))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            string json = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
        catch
        {
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private async Task Save(long chatId, Dictionary<string, string> routines, CancellationToken cancellationToken)
    {
        string path = storage.GetRoutinesPath(chatId);
        string json = JsonSerializer.Serialize(routines, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(path, json, Encoding.UTF8, cancellationToken);
    }

    private static string ExtractRoutineName(string text)
    {
        string cleaned = Regex.Replace(text, @"\b(crea|crear|guarda|guardar|nueva|nuevo|rutina|llamada|llamado|modo|hanna)\b", " ", RegexOptions.IgnoreCase);
        cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
        return Utilities.TextTools.Normalize(cleaned);
    }
}
