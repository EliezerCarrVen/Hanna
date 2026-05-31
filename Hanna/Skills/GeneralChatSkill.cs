using Hanna.Core;
using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class GeneralChatSkill : ISkill
{
    private readonly ModelOrchestrator orchestrator;
    private readonly PhaseService phaseService;
    private readonly TieredMemoryService tieredMemory;

    public GeneralChatSkill(ModelOrchestrator orchestrator, PhaseService phaseService, TieredMemoryService tieredMemory)
    {
        this.orchestrator = orchestrator;
        this.phaseService = phaseService;
        this.tieredMemory = tieredMemory;
    }

    public bool CanHandle(IntentResult intent) => intent.Type is IntentType.GeneralChat or IntentType.GeneralVerified or IntentType.TieredMemorySearch;

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (intent.Type == IntentType.TieredMemorySearch)
            return SkillResult.Text(tieredMemory.FormatSearchResult(originalText, intent.Limit <= 0 ? 5 : intent.Limit), true);

        string phase = phaseService.GetActivePhase();
        string localMemory = tieredMemory.BuildContext(originalText, 3);

        string augmentedText = originalText;
        string phaseInstruction = phaseService.BuildPhaseInstruction();
        if (!string.IsNullOrWhiteSpace(localMemory) || !string.IsNullOrWhiteSpace(phaseInstruction))
        {
            augmentedText = originalText + "\n\n[Instrucción de fase Hanna: no la muestres literalmente]\n" + phaseInstruction;
            if (!string.IsNullOrWhiteSpace(localMemory))
            {
                augmentedText += "\n\n[Memoria local recuperada como contexto interno. No copies este bloque ni lo muestres al usuario. Úsalo solo si ayuda a responder la pregunta.]\n" + localMemory;
            }
        }

        return await orchestrator.AnswerGeneral(chatId, augmentedText, intent.Type == IntentType.GeneralVerified, cancellationToken);
    }
}
