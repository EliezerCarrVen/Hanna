using Hanna.Models;
using Hanna.Services;
using Hanna.Utilities;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class AudioControlSkill : ISkill
{
    private readonly WindowsAudioSessionService audio;

    public AudioControlSkill(WindowsAudioSessionService audio)
    {
        this.audio = audio;
    }

    public bool CanHandle(IntentResult intent) => intent.Type == IntentType.AudioControl;

    public Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        string normalized = TextTools.Normalize(originalText);
        int percent = TextTools.ExtractNumber(originalText);

        if (percent <= 0 && Regex.IsMatch(normalized, @"\b(mutea|silencia|mudo)\b"))
            percent = 0;
        else if (percent <= 0)
            percent = 30;

        string app = ExtractApp(normalized);

        string result = string.IsNullOrWhiteSpace(app)
            ? audio.SetMasterVolume(percent)
            : audio.SetApplicationVolume(app, percent);

        return Task.FromResult(SkillResult.Text(result));
    }

    private static string ExtractApp(string normalized)
    {
        if (Regex.IsMatch(normalized, @"\bspotify\b")) return "spotify";
        if (Regex.IsMatch(normalized, @"\bchrome|navegador|google\b")) return "chrome";
        if (Regex.IsMatch(normalized, @"\bdiscord\b")) return "discord";
        if (Regex.IsMatch(normalized, @"\bobs\b")) return "obs";
        if (Regex.IsMatch(normalized, @"\bsistema|general|computadora|pc\b")) return "";
        return "";
    }
}
