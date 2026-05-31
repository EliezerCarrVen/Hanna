using Hanna.Core;

namespace Hanna.Services;

internal sealed class CourseNotebookService
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService runtime;

    public CourseNotebookService(AppConfig config, RuntimeSettingsService runtime)
    {
        this.config = config;
        this.runtime = runtime;
    }

    public string CreateOrUpdateNotebook(string subject, string source, string content)
    {
        subject = Sanitize(string.IsNullOrWhiteSpace(subject) ? "Materia general" : subject.Trim());
        string root = config.CourseNotebookDirectory;
        Directory.CreateDirectory(root);
        string dir = Path.Combine(root, subject);
        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(Path.Combine(dir, "fuentes"));

        Append(Path.Combine(dir, "tareas.md"), $"\n## {DateTime.Now:yyyy-MM-dd HH:mm} - {source}\n{content}\n");
        Ensure(Path.Combine(dir, "apuntes.md"), $"# {subject}\n\nApuntes generados por Hanna.\n");
        Ensure(Path.Combine(dir, "resumen_para_examen.md"), $"# Guía para examen final - {subject}\n\nAquí Hanna irá agregando resúmenes, conceptos clave y preguntas.\n");
        Ensure(Path.Combine(dir, "flashcards.md"), $"# Flashcards - {subject}\n\n");
        return dir;
    }

    public string[] ListNotebooks()
    {
        Directory.CreateDirectory(config.CourseNotebookDirectory);
        return Directory.GetDirectories(config.CourseNotebookDirectory).OrderBy(x => x).ToArray();
    }

    private static void Append(string path, string content)
    {
        File.AppendAllText(path, content, Encoding.UTF8);
    }

    private static void Ensure(string path, string content)
    {
        if (!File.Exists(path))
            File.WriteAllText(path, content, Encoding.UTF8);
    }

    private static string Sanitize(string value)
    {
        value = Regex.Replace(value, @"[^a-zA-Z0-9áéíóúÁÉÍÓÚñÑ _.-]+", "_").Trim();
        return string.IsNullOrWhiteSpace(value) ? "Materia" : value;
    }
}
