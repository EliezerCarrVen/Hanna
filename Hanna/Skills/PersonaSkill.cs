using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class PersonaSkill : ISkill
{
    private readonly HannaPersonaService personas;
    private readonly TokenEstimatorService estimator;
    private readonly TokenUsageLedgerService ledger;

    public PersonaSkill(HannaPersonaService personas, TokenEstimatorService estimator, TokenUsageLedgerService ledger)
    {
        this.personas = personas;
        this.estimator = estimator;
        this.ledger = ledger;
    }

    public bool CanHandle(IntentResult intent) => intent.Type == IntentType.SystemCommand;

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string text = (originalText ?? "").Trim();
        string normalized = text.ToLowerInvariant();

        if (Regex.IsMatch(normalized, @"^/(senior|architect|cto)\b"))
            return SkillResult.Text(await personas.SetActivePersona("senior", cancellationToken), true);

        if (Regex.IsMatch(normalized, @"^/(dev|engineer|developer)\b"))
            return SkillResult.Text(await personas.SetActivePersona("dev", cancellationToken), true);

        if (Regex.IsMatch(normalized, @"^/(ops|operator|devops)\b"))
            return SkillResult.Text(await personas.SetActivePersona("ops", cancellationToken), true);

        if (Regex.IsMatch(normalized, @"^/(analyst|analista)\b"))
            return SkillResult.Text(await personas.SetActivePersona("analyst", cancellationToken), true);

        if (normalized.StartsWith("/personas") || normalized.StartsWith("/persona lista"))
            return SkillResult.Text(personas.BuildListText(), true);

        if (normalized.StartsWith("/persona actual"))
        {
            HannaPersona p = personas.GetActivePersona();
            return SkillResult.Text(
                $"Persona actual: {p.DisplayName}\nModelo: {p.ModelName}\nHerramientas críticas: {(p.EnableHighComplexityTools ? "sí" : "no")}\nMax input: {p.MaxInputTokens:N0} tokens\nMax output: {p.MaxOutputTokens:N0} tokens",
                true);
        }

        if (normalized.StartsWith("/tokens hoy") || normalized.Equals("/tokens"))
            return SkillResult.Text(await ledger.BuildDailyReport(cancellationToken), true);

        if (normalized.StartsWith("/tokens archivo") || normalized.StartsWith("/estimar archivo"))
        {
            string path = ExtractQuoted(text);
            if (string.IsNullOrWhiteSpace(path))
                path = Regex.Replace(text, @"^/(tokens|estimar)\s+archivo", "", RegexOptions.IgnoreCase).Trim();

            TokenEstimateResult result = await estimator.EstimateFile(path, personas.GetActivePersona().ModelName, cancellationToken);
            return SkillResult.Text(result.ToHumanText(), true);
        }

        if (normalized.StartsWith("/tokens texto") || normalized.StartsWith("/estimar texto"))
        {
            string content = Regex.Replace(text, @"^/(tokens|estimar)\s+texto", "", RegexOptions.IgnoreCase).Trim();
            TokenEstimateResult result = estimator.EstimateText(content, "texto_manual", personas.GetActivePersona().ModelName);
            return SkillResult.Text(result.ToHumanText(), true);
        }

        return SkillResult.NotHandled();
    }

    private static string ExtractQuoted(string text)
    {
        var match = Regex.Match(text, "\"([^\"]+)\"");
        return match.Success ? match.Groups[1].Value.Trim() : "";
    }
}
