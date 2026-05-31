using Hanna.Models;
using Hanna.Services;
using Hanna.Utilities;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class PhaseSkill : ISkill
{
    private readonly PhaseService phaseService;

    public PhaseSkill(PhaseService phaseService)
    {
        this.phaseService = phaseService;
    }

    public bool CanHandle(IntentResult intent)
    {
        if (intent.Type == IntentType.PhaseControl)
            return true;
        if (intent.Type != IntentType.SystemCommand)
            return false;
        string q = TextTools.Normalize(intent.Query);
        return Regex.IsMatch(q, @"^(fases|fase|fase actual|fase local|fase ahorro|fase programacion|fase programación|fase multimedia|fase ops|fase estudio|fase nube|fase architect|fase arquitectura)\b");
    }

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string normalized = TextTools.Normalize(originalText);

        if (Regex.IsMatch(normalized, @"^fases\b|\bfases disponibles\b|\blista de fases\b"))
            return SkillResult.Text(BuildPhaseList(), true);

        if (Regex.IsMatch(normalized, @"fase actual|^fase\s*$|\bque fase\b|\bqué fase\b"))
            return SkillResult.Text("Fase actual de Hanna: " + phaseService.GetActivePhase() + "\n" + phaseService.BuildPhaseInstruction(), true);

        string phase = ExtractPhase(normalized);
        if (string.IsNullOrWhiteSpace(phase))
            return SkillResult.Text("No entendí qué fase quieres activar. Usa /fases para ver opciones.", true);

        phase = await phaseService.SetActivePhase(phase, cancellationToken);
        return SkillResult.Text("Fase cambiada a " + phase + ".\n" + phaseService.BuildPhaseInstruction(), true);
    }

    private static string BuildPhaseList()
    {
        return string.Join(Environment.NewLine, new[]
        {
            "Fases disponibles de Hanna:",
            "- local: Local / Offline. Resuelve con comandos locales y memoria antes de usar APIs.",
            "- ahorro: Ahorro. Minimiza tokens y prefiere respuestas cortas.",
            "- programacion: Programación. Prioriza código, proyectos y modelo coder local.",
            "- multimedia: Multimedia. Prioriza Spotify, YouTube, Netflix y TV LG.",
            "- ops: Operaciones. Sistema, respaldo, auditoría, estado y mantenimiento.",
            "- estudio: Estudio. Resúmenes, cuadernos, prácticas y explicaciones.",
            "- nube: Nube. Permite APIs externas explícitamente.",
            "- architect: Arquitectura. Análisis técnico profundo y diseño del proyecto.",
            "",
            "Ejemplos: /fase local, /fase programacion, /fase ahorro, /fase multimedia, /fase ops."
        });
    }

    private static string ExtractPhase(string normalized)
    {
        if (Regex.IsMatch(normalized, @"\b(local|offline)\b")) return "local";
        if (Regex.IsMatch(normalized, @"\b(ahorro|economia|económica|economica)\b")) return "ahorro";
        if (Regex.IsMatch(normalized, @"\b(programacion|programación|codigo|código|developer|dev)\b")) return "programacion";
        if (Regex.IsMatch(normalized, @"\b(multimedia|musica|música|netflix|spotify|youtube|tv)\b")) return "multimedia";
        if (Regex.IsMatch(normalized, @"\b(ops|operaciones|sistema|mantenimiento|backup)\b")) return "ops";
        if (Regex.IsMatch(normalized, @"\b(estudio|escuela|tarea|examen)\b")) return "estudio";
        if (Regex.IsMatch(normalized, @"\b(nube|cloud|api|openrouter)\b")) return "nube";
        if (Regex.IsMatch(normalized, @"\b(architect|arquitectura|arquitecto)\b")) return "architect";
        return "";
    }
}
