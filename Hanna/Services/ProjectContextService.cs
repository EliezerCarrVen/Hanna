using Hanna.Core;

namespace Hanna.Services;

internal sealed class ProjectContextService
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService? runtime;
    private static readonly string[] AllowedExtensions =
    {
        ".cs", ".csproj", ".sln", ".slnx", ".json", ".xml", ".xaml", ".sql", ".py", ".js", ".ts", ".html", ".css", ".md", ".txt", ".env.example"
    };

    private static readonly string[] IgnoredFolders =
    {
        "bin", "obj", ".vs", ".git", "node_modules", ".venv", "packages", "dist", "build", "target"
    };

    public ProjectContextService(AppConfig config, RuntimeSettingsService? runtime = null)
    {
        this.config = config;
        this.runtime = runtime;
    }

    public string BuildContextForRequest(string request)
    {
        RuntimeSettings? live = runtime?.Snapshot();
        string projectsDirectory = live?.ProjectsDirectory ?? config.ProjectsDirectory;
        if (string.IsNullOrWhiteSpace(projectsDirectory) || !Directory.Exists(projectsDirectory))
            return "No hay carpeta de proyectos configurada.";

        var files = Directory.GetFiles(projectsDirectory, "*", SearchOption.AllDirectories)
            .Where(IsAllowedFile)
            .Where(f => !HasIgnoredFolder(f))
            .OrderByDescending(f => ScoreFile(f, request))
            .ThenBy(f => f.Length)
            .Take(Math.Max(5, live?.ProjectIndexMaxFiles ?? config.ProjectIndexMaxFiles))
            .ToList();

        if (files.Count == 0)
            return "No encontré archivos analizables dentro de la carpeta de proyectos.";

        int remaining = Math.Max(4000, live?.ProjectIndexMaxChars ?? config.ProjectIndexMaxChars);
        var sb = new StringBuilder();
        sb.AppendLine("CONTEXTO DE PROYECTOS DEL USUARIO:");
        sb.AppendLine("Carpeta raíz: " + projectsDirectory);

        foreach (string file in files)
        {
            if (remaining <= 0)
                break;

            try
            {
                string relative = Path.GetRelativePath(projectsDirectory, file);
                string content = File.ReadAllText(file, Encoding.UTF8);
                content = content.Replace("\0", " ");

                int take = Math.Min(content.Length, Math.Min(4500, remaining));
                sb.AppendLine();
                sb.AppendLine($"--- ARCHIVO: {relative} ---");
                sb.AppendLine(content[..take]);

                remaining -= take;
            }
            catch
            {
            }
        }

        return sb.ToString();
    }

    private static bool IsAllowedFile(string path)
    {
        string ext = Path.GetExtension(path).ToLowerInvariant();
        return AllowedExtensions.Contains(ext);
    }

    private static bool HasIgnoredFolder(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(p => IgnoredFolders.Contains(p, StringComparer.OrdinalIgnoreCase));
    }

    private static int ScoreFile(string path, string request)
    {
        string name = Path.GetFileName(path).ToLowerInvariant();
        string normalized = Utilities.TextTools.Normalize(request ?? "");
        int score = 0;

        foreach (string word in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length >= 3))
        {
            if (name.Contains(word, StringComparison.OrdinalIgnoreCase))
                score += 6;
        }

        string ext = Path.GetExtension(path).ToLowerInvariant();
        if (normalized.Contains("c#") || normalized.Contains("csharp") || normalized.Contains("visual studio"))
        {
            if (ext == ".cs") score += 8;
            if (ext == ".csproj") score += 6;
        }
        if (normalized.Contains("sql") || normalized.Contains("base de datos"))
        {
            if (ext == ".sql") score += 10;
            if (name.Contains("db") || name.Contains("database")) score += 5;
        }
        if (name is "program.cs" or "appconfig.cs") score += 4;

        return score;
    }
}
