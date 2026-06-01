using Hanna.Core;

namespace Hanna.Services;

internal sealed class HannaDiagnosticsService
{
    private readonly AppConfig config;
    private readonly RuntimeSettingsService runtime;
    private readonly ModelModeService modelMode;
    private readonly PhaseService phaseService;
    private readonly RuntimeStatusService status;
    private readonly SafeLogService safeLogs;
    private readonly TokenUsageLedgerService tokenLedger;
    private readonly TieredMemoryService tieredMemory;
    private readonly HttpClient httpClient = new();

    public HannaDiagnosticsService(
        AppConfig config,
        RuntimeSettingsService runtime,
        ModelModeService modelMode,
        PhaseService phaseService,
        RuntimeStatusService status,
        SafeLogService safeLogs,
        TokenUsageLedgerService tokenLedger,
        TieredMemoryService tieredMemory)
    {
        this.config = config;
        this.runtime = runtime;
        this.modelMode = modelMode;
        this.phaseService = phaseService;
        this.status = status;
        this.safeLogs = safeLogs;
        this.tokenLedger = tokenLedger;
        this.tieredMemory = tieredMemory;
    }

    public async Task<string> BuildStatus(CancellationToken cancellationToken)
    {
        bool ollama = await IsOllamaAvailable(cancellationToken);
        var sb = new StringBuilder();
        sb.AppendLine("Estado profesional de Hanna");
        sb.AppendLine($"- Perfil HANNA_MODE: {config.HannaMode}");
        sb.AppendLine($"- Motor actual: {modelMode.GetModeLabel()}");
        sb.AppendLine($"- Fase actual: {phaseService.GetActivePhase()}");
        sb.AppendLine($"- Telegram texto: {(IsTelegramConfigured() ? "disponible" : "no disponible")}");
        sb.AppendLine($"- Telegram voz: {(IsTelegramConfigured() && !string.IsNullOrWhiteSpace(config.GroqApiKey) ? "disponible" : "no disponible")}");
        sb.AppendLine($"- Groq: {Configured(config.GroqApiKey)}");
        sb.AppendLine($"- Gemini: {Configured(config.GeminiApiKey)}");
        sb.AppendLine($"- OpenRouter: {Configured(config.OpenRouterApiKey)}");
        sb.AppendLine($"- Ollama: {(ollama ? "disponible" : "no disponible")}");
        sb.AppendLine($"- MongoDB: {(config.MongoEnabled ? status.GetStatus("MongoDB") : "desactivado")}");
        sb.AppendLine($"- TTS: {(EnvOff("HANNA_TTS_ENABLED") ? "no disponible" : "disponible/configurable")}");
        sb.AppendLine($"- Admin Web: {status.GetStatus("Admin Web")}");
        sb.AppendLine($"- WebChat: {status.GetStatus("WebChat standalone")}");
        sb.AppendLine($"- Mobile API: {status.GetStatus("Mobile API")}");
        sb.AppendLine($"- Último error: {safeLogs.GetLastError()}");
        sb.AppendLine("- Advertencias: " + BuildWarnings(ollama));
        return SecretSanitizer.Sanitize(sb.ToString().Trim());
    }

    public string BuildServices()
    {
        var sb = new StringBuilder();
        sb.AppendLine("Servicios registrados en este arranque:");
        foreach (var item in status.Snapshot())
            sb.AppendLine($"- {item.Name}: {item.State}. {item.Detail}");
        return SecretSanitizer.Sanitize(sb.ToString().Trim());
    }

    public async Task<string> BuildDemo(CancellationToken cancellationToken)
    {
        bool ollama = await IsOllamaAvailable(cancellationToken);
        return SecretSanitizer.Sanitize(string.Join(Environment.NewLine, new[]
        {
            "Demo segura de Hanna",
            $"1. Perfil activo: {config.HannaMode}.",
            $"2. Motor actual: {modelMode.GetModeLabel()}.",
            $"3. Fase actual: {phaseService.GetActivePhase()}.",
            $"4. Telegram texto: {(IsTelegramConfigured() ? "listo" : "pendiente de token")}.",
            $"5. Ollama: {(ollama ? "disponible" : "no disponible")}.",
            $"6. MongoDB: {(config.MongoEnabled ? status.GetStatus("MongoDB") : "desactivado")}.",
            $"7. Último error: {safeLogs.GetLastError()}",
            "Siguiente acción recomendada: ejecuta /siguiente_paso para priorizar la próxima mejora técnica."
        }));
    }

    public string BuildExecutiveSummary()
    {
        var active = status.Snapshot().Where(x => x.State == "activo").Select(x => x.Name).ToList();
        var skipped = status.Snapshot().Where(x => x.State == "omitido").Select(x => x.Name).ToList();
        var sb = new StringBuilder();
        sb.AppendLine("Resumen ejecutivo de Hanna");
        sb.AppendLine($"Estado general: Hanna está preparada para operar en modo {config.HannaMode} con motor {modelMode.GetModeLabel()}.");
        sb.AppendLine("Servicios activos: " + (active.Count == 0 ? "ninguno confirmado todavía" : string.Join(", ", active)) + ".");
        sb.AppendLine("Servicios omitidos: " + (skipped.Count == 0 ? "ninguno" : string.Join(", ", skipped)) + ".");
        sb.AppendLine("Riesgos: valida build local, credenciales opcionales y disponibilidad de Ollama/MongoDB según tu entorno.");
        sb.AppendLine("Siguiente paso: ejecutar scripts de pruebas finales y revisar /diagnostico.");
        return SecretSanitizer.Sanitize(sb.ToString().Trim());
    }

    public string BuildShowcase()
    {
        return SecretSanitizer.Sanitize(string.Join(Environment.NewLine, new[]
        {
            "Showcase Hanna",
            "- Control de motor con /motor actual y cambio seguro de proveedor.",
            "- Control de fase con /fase actual para adaptar comportamiento.",
            "- Diagnóstico operativo con /status y /diagnostico.",
            "- Telegram texto disponible sin exponer secretos.",
            "- Memoria y logs con sanitización defensiva.",
            "Funciones preparadas, no implementadas en esta fase: revisión avanzada de ZIP/proyectos con confirmación interactiva."
        }));
    }

    public string BuildNextStep()
    {
        if (!IsTelegramConfigured())
            return "Siguiente paso recomendado: configurar TELEGRAM_TOKEN en HannaEnv.env local y validar /status desde Telegram.";
        if (modelMode.GetModeLabel().Contains("Ollama", StringComparison.OrdinalIgnoreCase))
            return "Siguiente paso recomendado: validar disponibilidad de Ollama y el modelo local configurado antes de tareas largas.";
        return "Siguiente paso recomendado: agregar pruebas automatizadas de StartupProfile y comandos críticos /motor, /fase, /status y /demo.";
    }

    public string BuildProjectState()
    {
        string branch = TryGit("rev-parse", "--abbrev-ref", "HEAD");
        string statusText = TryGit("status", "--short");
        string changeState = string.IsNullOrWhiteSpace(statusText) ? "sin cambios pendientes" : "con cambios pendientes";
        return SecretSanitizer.Sanitize(string.Join(Environment.NewLine, new[]
        {
            "Estado del proyecto Hanna",
            "- Rama Git: " + (string.IsNullOrWhiteSpace(branch) ? "no detectable" : branch),
            "- Cambios: " + changeState,
            "- Último build conocido: no persistido por Hanna; ejecuta scripts/Probar-Hanna-Final.ps1.",
            "- Riesgos documentados: ver docs/RIESGOS_PENDIENTES.md.",
            "- Nota: este comando no ejecuta acciones peligrosas."
        }));
    }


    public string SearchMemory(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return "Uso: /memoria buscar TEXTO";

        return SecretSanitizer.Sanitize(tieredMemory.FormatSearchResult(query, 5));
    }

    public string BuildPreparedFeature(string command)
    {
        return command switch
        {
            "memoria_deduplicar" => "Preparada, no implementada: /memoria deduplicar requiere una rutina transaccional para revisar SQLite/JSONL sin perder datos.",
            "memoria_limpiar" => "Preparada, no implementada: /memoria limpiar_interna requiere respaldo previo y confirmación explícita.",
            "revisar_archivo" => "Preparada, no implementada: /revisar_archivo debe estimar tokens, listar riesgos y pedir confirmación antes de enviar a modelos externos.",
            "revisar_zip" => "Preparada, no implementada: /revisar_zip debe ignorar bin/obj/logs/tokens/memoria y bloquear secretos antes de analizar.",
            "revisar_proyecto" => "Preparada, no implementada: /revisar_proyecto debe generar inventario, estimación de tokens y confirmación por tamaño/costo.",
            "revisar_conversacion" => "Preparada, no implementada: /revisar_conversacion debe sanitizar nombres, tokens y prompts internos antes de resumir.",
            _ => "Preparada, no implementada."
        };
    }

    public async Task<string> BuildBudget(CancellationToken cancellationToken)
    {
        decimal usd = await tokenLedger.GetTodayEstimatedUsd(cancellationToken);
        return SecretSanitizer.Sanitize($"Control de costo\n- Motor actual: {modelMode.GetModeLabel()}\n- Fase activa: {phaseService.GetActivePhase()}\n- Gasto estimado hoy: ${usd:0.0000} USD\n- Presupuesto diario OpenRouter: ${config.OpenRouterDailyBudgetUsd:0.00} USD\n- Recomendación: estima tokens antes de archivos grandes con /tokens archivo \"RUTA\".");
    }

    private async Task<bool> IsOllamaAvailable(CancellationToken cancellationToken)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(2));
            string url = runtime.Snapshot().OllamaBaseUrl.TrimEnd('/') + "/api/tags";
            using var response = await httpClient.GetAsync(url, cts.Token);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private bool IsTelegramConfigured() => StartupProfile.Resolve(config, runtime.Snapshot(), modelMode.GetMode()).DecideTelegram().Enabled;
    private static string Configured(string value) => string.IsNullOrWhiteSpace(value) ? "no configurado" : "configurado";
    private static bool EnvOff(string key) => (Environment.GetEnvironmentVariable(key) ?? "true").Equals("false", StringComparison.OrdinalIgnoreCase);

    private string BuildWarnings(bool ollama)
    {
        var warnings = new List<string>();
        if (!IsTelegramConfigured()) warnings.Add("Telegram sin token válido");
        if (string.IsNullOrWhiteSpace(config.GroqApiKey)) warnings.Add("voz Telegram sin Groq");
        if (modelMode.GetModeLabel().Contains("Ollama", StringComparison.OrdinalIgnoreCase) && !ollama) warnings.Add("motor local seleccionado pero Ollama no responde");
        return warnings.Count == 0 ? "sin advertencias críticas" : string.Join("; ", warnings);
    }

    private static string TryGit(params string[] args)
    {
        try
        {
            var psi = new ProcessStartInfo("git")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            foreach (string arg in args) psi.ArgumentList.Add(arg);
            using var process = Process.Start(psi);
            if (process == null) return "";
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(1500);
            return output.Trim();
        }
        catch
        {
            return "";
        }
    }
}
