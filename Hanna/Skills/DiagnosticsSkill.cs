using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class DiagnosticsSkill : ISkill
{
    private readonly HannaDiagnosticsService diagnostics;
    private readonly SafeLogService logs;

    public DiagnosticsSkill(HannaDiagnosticsService diagnostics, SafeLogService logs)
    {
        this.diagnostics = diagnostics;
        this.logs = logs;
    }

    public bool CanHandle(IntentResult intent)
    {
        if (intent.Type != IntentType.SystemCommand)
            return false;

        string text = (intent.Query ?? "").Trim().ToLowerInvariant();
        return Regex.IsMatch(text, @"^/(status|health|diagnostico|diagnóstico|servicios|ultimo_error|último_error|logs|errores|demo|resumen_sistema|proyecto_estado|showcase|siguiente_paso|costo|presupuesto|limite|límite|modelos|memoria|revisar_archivo|revisar_zip|revisar_proyecto|revisar_conversacion)\b");
    }

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string text = (originalText ?? "").Trim().ToLowerInvariant();
        string answer;

        if (Regex.IsMatch(text, @"^/(status|health|diagnostico|diagnóstico)\b"))
            answer = await diagnostics.BuildStatus(cancellationToken);
        else if (Regex.IsMatch(text, @"^/servicios\b"))
            answer = diagnostics.BuildServices();
        else if (Regex.IsMatch(text, @"^/(ultimo_error|último_error|errores)\b"))
            answer = logs.GetLastError();
        else if (Regex.IsMatch(text, @"^/logs\b"))
            answer = logs.BuildLogsSummary();
        else if (Regex.IsMatch(text, @"^/demo\b"))
            answer = await diagnostics.BuildDemo(cancellationToken);
        else if (Regex.IsMatch(text, @"^/resumen_sistema\b"))
            answer = diagnostics.BuildExecutiveSummary();
        else if (Regex.IsMatch(text, @"^/proyecto_estado\b"))
            answer = diagnostics.BuildProjectState();
        else if (Regex.IsMatch(text, @"^/showcase\b"))
            answer = diagnostics.BuildShowcase();
        else if (Regex.IsMatch(text, @"^/siguiente_paso\b"))
            answer = diagnostics.BuildNextStep();
        else if (Regex.IsMatch(text, @"^/memoria\s+buscar\b"))
            answer = diagnostics.SearchMemory(Regex.Replace(originalText, @"^/memoria\s+buscar", "", RegexOptions.IgnoreCase).Trim());
        else if (Regex.IsMatch(text, @"^/memoria\s+deduplicar\b"))
            answer = diagnostics.BuildPreparedFeature("memoria_deduplicar");
        else if (Regex.IsMatch(text, @"^/memoria\s+limpiar_interna\b"))
            answer = diagnostics.BuildPreparedFeature("memoria_limpiar");
        else if (Regex.IsMatch(text, @"^/revisar_archivo\b"))
            answer = diagnostics.BuildPreparedFeature("revisar_archivo");
        else if (Regex.IsMatch(text, @"^/revisar_zip\b"))
            answer = diagnostics.BuildPreparedFeature("revisar_zip");
        else if (Regex.IsMatch(text, @"^/revisar_proyecto\b"))
            answer = diagnostics.BuildPreparedFeature("revisar_proyecto");
        else if (Regex.IsMatch(text, @"^/revisar_conversacion\b"))
            answer = diagnostics.BuildPreparedFeature("revisar_conversacion");
        else if (Regex.IsMatch(text, @"^/(costo|presupuesto|limite|límite|modelos)\b"))
            answer = await diagnostics.BuildBudget(cancellationToken);
        else
            return SkillResult.NotHandled();

        return SkillResult.Text(SecretSanitizer.Sanitize(answer), true);
    }
}
