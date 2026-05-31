using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class FileControllerService
{
    private readonly AppConfig config;
    private readonly TokenEstimatorService tokenEstimator;
    private readonly HannaPersonaService personaService;

    public FileControllerService(AppConfig config, TokenEstimatorService tokenEstimator, HannaPersonaService personaService)
    {
        this.config = config;
        this.tokenEstimator = tokenEstimator;
        this.personaService = personaService;
    }

    public string ListFiles(string text)
    {
        string path = ExtractPath(text);
        if (string.IsNullOrWhiteSpace(path))
            path = config.BaseDirectory;

        if (!Directory.Exists(path))
            return "No encontré esa carpeta.";

        var entries = Directory.GetFileSystemEntries(path)
            .Take(30)
            .Select(Path.GetFileName);

        return "Archivos encontrados:\n" + string.Join("\n", entries);
    }

    public async Task<string> ReadFile(string text, CancellationToken cancellationToken)
    {
        string path = ExtractPath(text);

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return "No encontré el archivo para leer.";

        TokenEstimateResult estimate = await tokenEstimator.EstimateFile(path, personaService.GetActivePersona().ModelName, cancellationToken);
        string estimateText = estimate.ToHumanText();

        if (estimate.ExceedsRecommendedLimit)
        {
            return estimateText + "\n\nNo lo voy a cargar completo automáticamente para evitar gasto de tokens. Si quieres, pide: resume secciones del archivo o indexa este archivo localmente.";
        }

        string content = await File.ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);
        string preview = content.Length <= 3000 ? content : content[..3000] + "\n\n[Contenido recortado]";

        return estimateText + "\n\nVista previa:\n" + preview;
    }

    public async Task<string> WriteFile(string text, CancellationToken cancellationToken)
    {
        var match = Regex.Match(text, @"archivo\s+(.+?)\s+con\s+(.+)$", RegexOptions.IgnoreCase);

        if (!match.Success)
            return "Para crear un archivo dime: crear archivo nombre.txt con contenido.";

        string name = match.Groups[1].Value.Trim();
        string content = match.Groups[2].Value.Trim();
        string path = Path.Combine(config.BaseDirectory, name);

        await File.WriteAllTextAsync(path, content, Encoding.UTF8, cancellationToken);

        TokenEstimateResult estimate = tokenEstimator.EstimateText(content, path, personaService.GetActivePersona().ModelName);

        return $"Archivo creado: {path}\n\n{estimate.ToHumanText()}";
    }

    public string FindFile(string text)
    {
        string query = ExtractPath(text);

        if (string.IsNullOrWhiteSpace(query))
            return "Dime qué archivo quieres buscar.";

        var files = Directory.GetFiles(config.BaseDirectory, "*", SearchOption.AllDirectories)
            .Where(f => Path.GetFileName(f).Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(20);

        return "Resultados:\n" + string.Join("\n", files);
    }

    private static string ExtractPath(string text)
    {
        var quoted = Regex.Match(text, "\"([^\"]+)\"");

        if (quoted.Success)
            return quoted.Groups[1].Value.Trim();

        string clean = text;
        string[] remove = { "lista archivos", "listar archivos", "ver archivos", "leer archivo", "crear archivo", "escribir archivo", "buscar archivo", "estimar tokens archivo", "tokens archivo", "archivo", "carpeta" };

        foreach (string phrase in remove)
            clean = Regex.Replace(clean, Regex.Escape(phrase), " ", RegexOptions.IgnoreCase);

        return Regex.Replace(clean, @"\s+", " ").Trim();
    }
}
