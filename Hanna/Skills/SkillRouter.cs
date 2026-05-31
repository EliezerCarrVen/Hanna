using Hanna.Core;
using Hanna.Models;
using Hanna.Services;
using Hanna.Utilities;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class SkillRouter
{
    private readonly IntentRouter intentRouter;
    private readonly IReadOnlyList<ISkill> skills;
    private readonly MultiCommandParser multiCommandParser;
    private readonly AuditTrailService? audit;

    public SkillRouter(IntentRouter intentRouter, IReadOnlyList<ISkill> skills, AuditTrailService? audit = null)
    {
        this.intentRouter = intentRouter;
        this.skills = skills;
        this.audit = audit;
        multiCommandParser = new MultiCommandParser();
    }

    public async Task<SkillResult> Route(long chatId, string text, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string rawText = text?.Trim() ?? "";

        if (string.IsNullOrWhiteSpace(rawText))
            return SkillResult.NotHandled();

        if (multiCommandParser.IsStopCommand(rawText))
            return SkillResult.Text("Me detengo.", true);

        if (Regex.IsMatch(rawText, @"^/\s*\S+"))
            return await RouteSingle(chatId, rawText, botClient, cancellationToken);

        IReadOnlyList<string> commands = multiCommandParser.Split(rawText);

        if (commands.Count > 1)
        {
            var responses = new List<string>();

            foreach (string command in commands)
            {
                cancellationToken.ThrowIfCancellationRequested();

                SkillResult partial = await RouteSingle(chatId, command, botClient, cancellationToken);

                if (partial.Handled && !partial.SkipResponse && !string.IsNullOrWhiteSpace(partial.ResponseText))
                    responses.Add(partial.ResponseText.Trim());
            }

            if (responses.Count == 0)
                return SkillResult.Text("Listo. Ejecuté las órdenes que pude procesar.", true);

            return SkillResult.Text("Listo. Ejecuté " + responses.Count + " órdenes:\n" + string.Join("\n", responses.Select((x, i) => $"{i + 1}. {x}")), true);
        }

        return await RouteSingle(chatId, rawText, botClient, cancellationToken);
    }


    private async Task<SkillResult> RouteSingle(long chatId, string rawText, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string? nameWarning = DetectWrongAssistantName(rawText);
        if (!string.IsNullOrWhiteSpace(nameWarning))
        {
            await Audit(chatId, "name_guard", rawText, true, cancellationToken);
            return SkillResult.Text(nameWarning, true);
        }

        if (Regex.IsMatch(rawText, @"^/\s*\S+"))
        {
            string commandText = NormalizeCommand(rawText);

            IntentType commandType = DetectSlashCommandType(commandText);

            var commandIntent = new IntentResult(
                commandType,
                commandText,
                TextTools.ExtractNumber(commandText),
                TextTools.ExtractDevice(commandText));

            Console.WriteLine($"[Router] Command intent: {commandIntent.Type} | {commandText}");

            foreach (var skill in skills)
            {
                if (skill.CanHandle(commandIntent))
                {
                    SkillResult commandResult = await skill.Handle(chatId, commandText, commandIntent, botClient, cancellationToken);
                    Console.WriteLine($"[Router] Command skill: {skill.GetType().Name} | handled={commandResult.Handled} | len={(commandResult.ResponseText ?? "").Length}");

                    if (commandResult.Handled)
                        return commandResult;
                }
            }

            return SkillResult.Text("No reconozco ese comando. Usa /h para ver la lista disponible.", true);
        }

        IntentResult intent = intentRouter.Classify(rawText);
        Console.WriteLine($"[Router] Intent: {intent.Type} | Query: {intent.Query}");
        await Audit(chatId, intent.Type.ToString(), rawText, true, cancellationToken);

        foreach (var skill in skills)
        {
            if (skill.CanHandle(intent))
            {
                SkillResult result = await skill.Handle(chatId, rawText, intent, botClient, cancellationToken);
                Console.WriteLine($"[Router] Skill: {skill.GetType().Name} | handled={result.Handled} | skip={result.SkipResponse} | len={(result.ResponseText ?? "").Length}");

                if (result.Handled)
                    return result;
            }
        }

        GeneralChatSkill? general = skills.OfType<GeneralChatSkill>().FirstOrDefault();

        if (general != null)
        {
            Console.WriteLine("[Router] Ninguna skill respondió. Reenrutando a GeneralChatSkill.");
            var fallbackIntent = new IntentResult(
                IntentType.GeneralChat,
                rawText,
                TextTools.ExtractNumber(rawText),
                TextTools.ExtractDevice(rawText));

            SkillResult fallback = await general.Handle(chatId, rawText, fallbackIntent, botClient, cancellationToken);
            Console.WriteLine($"[Router] General fallback | handled={fallback.Handled} | len={(fallback.ResponseText ?? "").Length}");

            if (fallback.Handled)
                return fallback;
        }

        return SkillResult.Text("Te leí, pero no encontré una skill ni un motor disponible para responder. Revisa que GeneralChatSkill esté registrado en Program.cs.", true);
    }

    private async Task Audit(long chatId, string action, string detail, bool success, CancellationToken cancellationToken)
    {
        if (audit != null)
            await audit.AppendAsync(chatId, "SkillRouter", action, detail, success, cancellationToken);
    }

    private static string? DetectWrongAssistantName(string text)
    {
        string normalized = TextTools.Normalize(text);
        bool startsWithWrongName = Regex.IsMatch(normalized, @"^(oye\s+)?(alexa|siri|cortana|jarvis|copilot|google)\b");
        bool engineCommand = ModelModeService.IsEngineModeCommandText(text);
        if (!startsWithWrongName || engineCommand)
            return null;

        Match m = Regex.Match(normalized, @"^(oye\s+)?(?<name>alexa|siri|cortana|jarvis|copilot|google)\b");
        string name = m.Success ? m.Groups["name"].Value : "ese nombre";
        return $"¿{name}? No. Soy Hanna. Corrige el nombre y con gusto te ayudo.";
    }

    private static IntentType DetectSlashCommandType(string commandText)
    {
        string raw = (commandText ?? "").Trim().ToLowerInvariant();
        string normalized = TextTools.Normalize(commandText);

        if (Regex.IsMatch(raw, @"^/(motor|motores)\b") ||
            Regex.IsMatch(raw, @"^/modo\s+(ollama|local|gemini|groq|hibrido|híbrido|openrouter|open\s+router|original)\b") ||
            Regex.IsMatch(normalized, @"^(motor|motores)\b"))
            return IntentType.EngineModeChange;

        if (Regex.IsMatch(raw, @"^/(fase|fases)\b") || Regex.IsMatch(normalized, @"^(fase|fases)\b"))
            return IntentType.PhaseControl;

        return IntentType.SystemCommand;
    }

    private static string NormalizeCommand(string text)
    {
        text = text.Trim();
        text = Regex.Replace(text, @"^/\s+", "/");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }
}
