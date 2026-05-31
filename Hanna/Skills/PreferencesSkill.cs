using Hanna.Models;
using Hanna.Services;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class PreferencesSkill : ISkill
{
    private readonly PreferencesService preferences;

    public PreferencesSkill(PreferencesService preferences)
    {
        this.preferences = preferences;
    }

    public bool CanHandle(IntentResult intent) => intent.Type is IntentType.PreferenceSet or IntentType.PreferenceShow;

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (intent.Type == IntentType.PreferenceShow)
            return SkillResult.Text(await preferences.Show(chatId, cancellationToken), true);

        string normalized = Utilities.TextTools.Normalize(originalText);

        if (normalized.Contains("playlist"))
        {
            string playlist = ExtractPlaylistPreference(originalText);

            if (string.IsNullOrWhiteSpace(playlist))
                return SkillResult.Text("Dime cuál playlist quieres guardar como preferida.", true);

            await preferences.Set(chatId, "spotify_playlist_preferida", playlist, cancellationToken);
            return SkillResult.Text($"Listo. Dejé {playlist} como tu playlist preferida.", true);
        }

        if (normalized.Contains("dispositivo"))
        {
            string device = ExtractDevicePreference(originalText);

            if (string.IsNullOrWhiteSpace(device))
                return SkillResult.Text("Dime cuál dispositivo quieres guardar como preferido.", true);

            await preferences.Set(chatId, "spotify_dispositivo_preferido_nombre", device, cancellationToken);
            return SkillResult.Text($"Listo. Dejé {device} como dispositivo preferido.", true);
        }

        await preferences.Set(chatId, "nota", originalText, cancellationToken);
        return SkillResult.Text("Listo. Guardé esa preferencia.", true);
    }

    private static string ExtractPlaylistPreference(string text)
    {
        var match = Regex.Match(text, @"playlist\s+(?:preferida|favorita)?\s*(?:es|sea|como)?\s*(.+)$", RegexOptions.IgnoreCase);
        string value = match.Success ? match.Groups[1].Value : text;
        value = Regex.Replace(value, @"\b(mi|playlist|preferida|favorita|es|sea|como|guarda|guardar|preferencia)\b", " ", RegexOptions.IgnoreCase);
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static string ExtractDevicePreference(string text)
    {
        var match = Regex.Match(text, @"dispositivo\s+(?:preferido|favorito)?\s*(?:es|sea|como)?\s*(.+)$", RegexOptions.IgnoreCase);
        string value = match.Success ? match.Groups[1].Value : text;
        value = Regex.Replace(value, @"\b(mi|dispositivo|preferido|favorito|es|sea|como|guarda|guardar|preferencia)\b", " ", RegexOptions.IgnoreCase);
        return Regex.Replace(value, @"\s+", " ").Trim();
    }
}
