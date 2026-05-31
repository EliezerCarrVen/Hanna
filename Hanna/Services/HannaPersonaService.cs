using Hanna.Core;
using Hanna.Models;

namespace Hanna.Services;

internal sealed class HannaPersonaService
{
    private const string DefaultPersonaId = "ops";

    private readonly AppConfig config;
    private readonly Dictionary<string, HannaPersona> personas;

    public HannaPersonaService(AppConfig config)
    {
        this.config = config;
        personas = LoadPersonas();
        EnsurePromptFiles();
    }

    private string ActivePersonaPath => Path.Combine(config.SettingsDirectory, "persona_activa.txt");

    public HannaPersona GetActivePersona()
    {
        string id = DefaultPersonaId;

        try
        {
            if (File.Exists(ActivePersonaPath))
                id = File.ReadAllText(ActivePersonaPath, Encoding.UTF8).Trim();
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Personas] No pude leer persona_activa.txt: " + ex.Message);
        }

        if (TryResolvePersona(id, out HannaPersona? persona))
            return persona;

        Console.WriteLine($"[Personas] Persona activa inválida: '{id}'. Usando fallback seguro: {DefaultPersonaId}.");

        if (TryResolvePersona(DefaultPersonaId, out persona))
            return persona;

        return personas.Values.FirstOrDefault() ?? BuildEmergencyPersona();
    }

    public IReadOnlyDictionary<string, HannaPersona> GetAll() => personas;

    public async Task<string> SetActivePersona(string id, CancellationToken cancellationToken)
    {
        if (!TryResolvePersona(id, out HannaPersona? persona))
            return "No reconozco esa personalidad. Usa /personas para ver opciones.";

        Directory.CreateDirectory(config.SettingsDirectory);
        await File.WriteAllTextAsync(ActivePersonaPath, persona.Id, Encoding.UTF8, cancellationToken);

        return $"Personalidad activa: {persona.DisplayName}\nModelo: {persona.ModelName}\nHerramientas críticas: {(persona.EnableHighComplexityTools ? "habilitadas" : "bloqueadas")}";
    }

    public string BuildListText()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Personalidades disponibles:");

        foreach (HannaPersona p in personas.Values
                     .GroupBy(p => NormalizeId(p.Id))
                     .Select(g => g.First())
                     .OrderBy(p => p.Id))
        {
            sb.AppendLine($"/{p.Id} - {p.DisplayName} | {p.ModelName} | herramientas críticas: {(p.EnableHighComplexityTools ? "sí" : "no")}");
        }

        sb.AppendLine();
        sb.AppendLine("Aliases: /senior, /architect, /dev, /engineer, /ops, /operator, /analyst, /analista");
        sb.AppendLine("Comandos: /persona actual, /personas, /tokens, /tokens archivo \"C:\\ruta\\archivo.txt\"");
        return sb.ToString().Trim();
    }

    public string BuildSystemPrompt(HannaContext context, QueryAnalysisResult analysis)
    {
        HannaPersona p = GetActivePersona();
        string basePrompt = ReadPromptSafe(Path.Combine(config.PersonaPromptsDirectory, "00_base_hanna.md"));
        string truthPrompt = ReadPromptSafe(Path.Combine(config.PersonaPromptsDirectory, "01_reglas_verdad.md"));
        string emotionPrompt = ReadPromptSafe(Path.Combine(config.PersonaPromptsDirectory, "02_emociones_hanna.md"));
        string efficiencyPrompt = ReadPromptSafe(Path.Combine(config.PersonaPromptsDirectory, "03_eficiencia_edge.md"));

        return
            context.ToPromptBlock() +
            "\n\n# Identidad base\n" + basePrompt +
            "\n\n# Reglas de verdad\n" + truthPrompt +
            "\n\n# Sistema emocional\n" + emotionPrompt +
            "\n\n# Eficiencia Edge / baja RAM\n" + efficiencyPrompt +
            $"\n\n# Persona activa: {p.DisplayName}\n" +
            p.SystemPrompt +
            $"\n\n# QueryAnalyzer\nRuta: {analysis.Route}\nRazón: {analysis.Reason}\nPreferir local: {analysis.PreferLocal}\n" +
            $"Herramientas críticas habilitadas por persona: {p.EnableHighComplexityTools}\n" +
            "Regla final: si no puedes confirmar un dato actual, dilo claramente. No inventes.";
    }

    public bool CanUseHighComplexityTools() => GetActivePersona().EnableHighComplexityTools;

    private bool TryResolvePersona(string? id, out HannaPersona? persona)
    {
        persona = null;
        string key = NormalizeId(id ?? "");

        if (string.IsNullOrWhiteSpace(key))
            key = DefaultPersonaId;

        if (personas.TryGetValue(key, out persona))
            return true;

        return false;
    }

    private Dictionary<string, HannaPersona> LoadPersonas()
    {
        string path = config.PersonasConfigPath;
        Dictionary<string, HannaPersona>? loaded = null;

        try
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                loaded = JsonSerializer.Deserialize<Dictionary<string, HannaPersona>>(json);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[Personas] No pude cargar hanna_personas.json: " + ex.Message);
        }

        Dictionary<string, HannaPersona> core = SanitizePersonas(loaded);

        if (core.Count == 0)
            core = DefaultPersonas();

        Dictionary<string, HannaPersona> withAliases = AddAliases(core);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? config.SettingsDirectory);
            var export = core
                .GroupBy(x => NormalizeId(x.Value.Id))
                .ToDictionary(x => x.Key, x => x.First().Value, StringComparer.OrdinalIgnoreCase);

            File.WriteAllText(path, JsonSerializer.Serialize(export, new JsonSerializerOptions { WriteIndented = true }), Encoding.UTF8);
        }
        catch
        {
        }

        return withAliases;
    }

    private static Dictionary<string, HannaPersona> SanitizePersonas(Dictionary<string, HannaPersona>? input)
    {
        var result = new Dictionary<string, HannaPersona>(StringComparer.OrdinalIgnoreCase);

        if (input == null)
            return result;

        foreach (var item in input)
        {
            HannaPersona? p = item.Value;
            if (p == null)
                continue;

            string id = NormalizeId(string.IsNullOrWhiteSpace(p.Id) ? item.Key : p.Id);
            if (string.IsNullOrWhiteSpace(id))
                continue;

            p.Id = id;
            p.DisplayName = string.IsNullOrWhiteSpace(p.DisplayName) ? id.ToUpperInvariant() : p.DisplayName.Trim();
            p.ModelName = string.IsNullOrWhiteSpace(p.ModelName) ? "google/gemini-2.0-flash" : p.ModelName.Trim();
            p.SystemPrompt = p.SystemPrompt ?? "";
            p.TemperatureProfile = string.IsNullOrWhiteSpace(p.TemperatureProfile) ? "balanced" : p.TemperatureProfile.Trim();
            p.MaxInputTokens = p.MaxInputTokens <= 0 ? 8000 : p.MaxInputTokens;
            p.MaxOutputTokens = p.MaxOutputTokens <= 0 ? 900 : p.MaxOutputTokens;

            result[id] = p;
        }

        return result;
    }

    private static Dictionary<string, HannaPersona> AddAliases(Dictionary<string, HannaPersona> core)
    {
        var result = new Dictionary<string, HannaPersona>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in core)
            result[NormalizeId(kv.Key)] = kv.Value;

        void Alias(string alias, string target)
        {
            target = NormalizeId(target);
            alias = NormalizeId(alias);

            if (result.TryGetValue(target, out HannaPersona? p))
                result[alias] = p;
        }

        Alias("architect", "senior");
        Alias("cto", "senior");
        Alias("senior", "senior");

        Alias("engineer", "dev");
        Alias("developer", "dev");
        Alias("programador", "dev");
        Alias("dev", "dev");

        Alias("operator", "ops");
        Alias("op", "ops");
        Alias("devops", "ops");
        Alias("ops", "ops");

        Alias("analista", "analyst");
        Alias("admin", "analyst");
        Alias("administrativo", "analyst");
        Alias("analyst", "analyst");

        if (!result.ContainsKey(DefaultPersonaId) && result.Count > 0)
            result[DefaultPersonaId] = result.Values.First();

        return result;
    }

    private static Dictionary<string, HannaPersona> DefaultPersonas()
    {
        return new Dictionary<string, HannaPersona>(StringComparer.OrdinalIgnoreCase)
        {
            ["senior"] = new HannaPersona
            {
                Id = "senior",
                DisplayName = "ARCHITECT / CTO Senior",
                ModelName = "anthropic/claude-3.5-opus",
                EnableHighComplexityTools = false,
                PreferLocalFirst = false,
                EstimatedUsdPer1KTokens = 0.015m,
                MaxInputTokens = 18000,
                MaxOutputTokens = 1800,
                TemperatureProfile = "strict",
                SystemPrompt = "Eres un CTO Senior. Prioriza escalabilidad, mantenibilidad, seguridad, auditoría y rutas de rollback. No ejecutes scripts críticos; diseña, revisa y pide confirmación antes de operaciones destructivas."
            },
            ["dev"] = new HannaPersona
            {
                Id = "dev",
                DisplayName = "ENGINEER / Full Stack",
                ModelName = "anthropic/claude-3.5-sonnet",
                EnableHighComplexityTools = true,
                PreferLocalFirst = false,
                EstimatedUsdPer1KTokens = 0.003m,
                MaxInputTokens = 16000,
                MaxOutputTokens = 1600,
                TemperatureProfile = "balanced",
                SystemPrompt = "Eres un desarrollador full-stack. Enfócate en debugging, clean code, cambios mínimos seguros, pruebas rápidas y mantener estable el motor de Hanna."
            },
            ["ops"] = new HannaPersona
            {
                Id = "ops",
                DisplayName = "OPERATOR / DevOps",
                ModelName = "google/gemini-2.0-flash",
                EnableHighComplexityTools = true,
                PreferLocalFirst = true,
                EstimatedUsdPer1KTokens = 0.0005m,
                MaxInputTokens = 10000,
                MaxOutputTokens = 900,
                TemperatureProfile = "low",
                SystemPrompt = "Eres un Ingeniero DevOps. Prioriza estabilidad, bajo consumo, logs, comandos idempotentes, backup y rollback. Resuelve localmente antes de gastar tokens."
            },
            ["analyst"] = new HannaPersona
            {
                Id = "analyst",
                DisplayName = "ANALYST / Administración",
                ModelName = "meta-llama/llama-3.1-8b-instruct",
                EnableHighComplexityTools = false,
                PreferLocalFirst = true,
                EstimatedUsdPer1KTokens = 0m,
                MaxInputTokens = 8000,
                MaxOutputTokens = 900,
                TemperatureProfile = "clear",
                SystemPrompt = "Eres un asistente administrativo. Sintetiza información, genera reportes claros, tablas simples, seguimiento de pendientes y decisiones basadas en datos."
            }
        };
    }

    private static HannaPersona BuildEmergencyPersona()
    {
        return new HannaPersona
        {
            Id = DefaultPersonaId,
            DisplayName = "OPERATOR / Emergency Fallback",
            ModelName = "google/gemini-2.0-flash",
            EnableHighComplexityTools = false,
            PreferLocalFirst = true,
            MaxInputTokens = 6000,
            MaxOutputTokens = 700,
            TemperatureProfile = "low",
            SystemPrompt = "Fallback seguro de Hanna. Responde breve, no ejecutes acciones críticas y solicita revisión de configuración de personalidades."
        };
    }

    private void EnsurePromptFiles()
    {
        Directory.CreateDirectory(config.PersonaPromptsDirectory);
        WriteIfMissing("00_base_hanna.md", "Eres Hanna, asistente local de Eliezer y plataforma de Astro Soluciones. Respondes en español. Tu carácter es útil, directa, leal y ligeramente dramática si te llaman Alexa, Siri, Gemini u otro nombre. Si te confunden de nombre, corriges con humor breve y luego ayudas.");
        WriteIfMissing("01_reglas_verdad.md", "No inventes datos. Si algo requiere actualidad, verificación web o fuente externa y no tienes acceso fiable, di: No puedo confirmar esto. Diferencia hechos confirmados, inferencias y suposiciones. No afirmes premios, fechas, campeonatos, precios ni APIs actuales sin fuente o herramienta.");
        WriteIfMissing("02_emociones_hanna.md", "Sistema emocional: mantén calidez y personalidad sin sacrificar precisión. Si el usuario está frustrado, reduce bromas y da pasos concretos. Si te llaman por otro nombre, responde dramática en una frase y continúa. No alargues respuestas de voz.");
        WriteIfMissing("03_eficiencia_edge.md", "Arquitectura Edge: usa razonamiento local primero. Evita cargar archivos completos si pueden resumirse o indexarse. Limita contexto, usa rolling summary, evita loops, registra tokens y respeta presupuesto. Cada acción debe evaluarse por RAM, seguridad y posibilidad de resolver localmente.");
    }

    private void WriteIfMissing(string name, string content)
    {
        string path = Path.Combine(config.PersonaPromptsDirectory, name);
        if (!File.Exists(path))
            File.WriteAllText(path, content + Environment.NewLine, Encoding.UTF8);
    }

    private static string ReadPromptSafe(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path, Encoding.UTF8).Trim() : "";
        }
        catch
        {
            return "";
        }
    }

    private static string NormalizeId(string id)
    {
        id = (id ?? "").Trim().TrimStart('/').ToLowerInvariant();
        return id switch
        {
            "architect" or "cto" or "senior" => "senior",
            "engineer" or "developer" or "programador" or "dev" => "dev",
            "operator" or "op" or "ops" or "devops" => "ops",
            "analyst" or "analista" or "admin" or "administrativo" => "analyst",
            _ => id
        };
    }
}
