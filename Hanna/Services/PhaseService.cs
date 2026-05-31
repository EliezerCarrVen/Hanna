using Hanna.Core;

namespace Hanna.Services;

internal sealed class PhaseService
{
    private static readonly HashSet<string> ValidPhases = new(StringComparer.OrdinalIgnoreCase)
    {
        "local", "estudio", "programacion", "programación", "ops", "multimedia", "ahorro", "nube", "architect", "arquitectura"
    };

    private readonly AppConfig config;

    public PhaseService(AppConfig config)
    {
        this.config = config;
    }

    public string GetActivePhase()
    {
        try
        {
            if (!File.Exists(config.ActivePhasePath))
                return "local";

            string value = File.ReadAllText(config.ActivePhasePath, Encoding.UTF8).Trim().ToLowerInvariant();
            return Normalize(value);
        }
        catch
        {
            return "local";
        }
    }

    public async Task<string> SetActivePhase(string phase, CancellationToken cancellationToken)
    {
        string normalized = Normalize(phase);
        Directory.CreateDirectory(Path.GetDirectoryName(config.ActivePhasePath) ?? config.SettingsDirectory);
        await File.WriteAllTextAsync(config.ActivePhasePath, normalized, Encoding.UTF8, cancellationToken);
        return normalized;
    }

    public IReadOnlyList<object> ListPhases()
    {
        return new object[]
        {
            new { id = "local", label = "Local / Offline", description = "Resuelve con comandos locales y memoria antes de usar APIs." },
            new { id = "ahorro", label = "Ahorro", description = "Minimiza tokens y prefiere respuestas cortas." },
            new { id = "programacion", label = "Programación", description = "Prioriza código, proyectos y modelo coder local." },
            new { id = "multimedia", label = "Multimedia", description = "Prioriza Spotify, YouTube, Netflix y TV LG." },
            new { id = "ops", label = "Operaciones", description = "Sistema, respaldo, auditoría, estado y mantenimiento." },
            new { id = "estudio", label = "Estudio", description = "Resúmenes, cuadernos, prácticas y explicaciones." },
            new { id = "nube", label = "Nube", description = "Permite APIs externas explícitamente." },
            new { id = "architect", label = "Arquitectura", description = "Análisis técnico profundo y diseño del proyecto." }
        };
    }

    public string BuildPhaseInstruction()
    {
        return GetActivePhase() switch
        {
            "local" => "Fase local: resuelve primero con memoria, archivos y skills locales. Evita APIs si no son necesarias.",
            "ahorro" => "Fase ahorro: responde breve, usa pocos tokens y evita contexto innecesario.",
            "programacion" => "Fase programación: prioriza precisión técnica, código ejecutable y revisión de proyectos.",
            "multimedia" => "Fase multimedia: prioriza acciones de Spotify, YouTube, Netflix, TV LG y reproducción.",
            "ops" => "Fase operaciones: prioriza estado del sistema, respaldos, logs, seguridad y mantenimiento.",
            "estudio" => "Fase estudio: explica paso a paso, con lenguaje claro y útil para tareas escolares.",
            "nube" => "Fase nube: permite usar proveedores externos cuando el usuario lo pida o la tarea lo requiera.",
            "architect" => "Fase arquitectura: diseña soluciones robustas, seguras y eficientes antes de implementar.",
            _ => "Fase local: prioriza recursos locales."
        };
    }

    private static string Normalize(string phase)
    {
        string value = (phase ?? "").Trim().ToLowerInvariant();
        value = value.Replace("programación", "programacion").Replace("arquitectura", "architect");
        if (!ValidPhases.Contains(value))
            return "local";
        return value;
    }
}
