using Hanna.Models;
using Hanna.Services;
using Hanna.Spotify;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class RoutineSkill : ISkill
{
    private readonly RoutineService routines;
    private readonly PreferencesService preferences;
    private readonly ResponseService response;
    private readonly SpotifyLibraryService library;
    private readonly SpotifyPlaybackService playback;

    public RoutineSkill(RoutineService routines, PreferencesService preferences, ResponseService response, SpotifyLibraryService library, SpotifyPlaybackService playback)
    {
        this.routines = routines;
        this.preferences = preferences;
        this.response = response;
        this.library = library;
        this.playback = playback;
    }

    public bool CanHandle(IntentResult intent) => intent.Type is IntentType.RoutineRun or IntentType.RoutineCreate or IntentType.RoutineList;

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (intent.Type == IntentType.RoutineCreate)
            return SkillResult.Text(await routines.CreateRoutine(chatId, originalText, cancellationToken), true);

        if (intent.Type == IntentType.RoutineList)
            return SkillResult.Text(await routines.ListRoutines(chatId, cancellationToken), true);

        string routine = routines.DetectRoutineName(originalText);

        if (routine == "estudio")
        {
            await response.SetMode(chatId, "texto", cancellationToken);
            return SkillResult.Text("Modo estudio activado. Responderé en texto para no interrumpir.", true);
        }

        if (routine == "noche")
        {
            await playback.Pause(chatId, cancellationToken);
            await response.SetMode(chatId, "texto", cancellationToken);
            return SkillResult.Text("Buenas noches. Pausé Spotify y dejé las respuestas en texto.", true);
        }

        if (routine == "musica")
        {
            await response.SetMode(chatId, "ambos", cancellationToken);

            string playlistName = await preferences.Get(chatId, "spotify_playlist_preferida", cancellationToken);

            if (!string.IsNullOrWhiteSpace(playlistName))
            {
                var playlist = await library.FindPlaylist(chatId, playlistName, cancellationToken);

                if (playlist != null)
                {
                    var result = await playback.PlayPlaylist(chatId, playlist.Id, "", cancellationToken);

                    if (result.Success)
                        return SkillResult.Text($"Modo música activado. Reproduciendo tu playlist preferida: {playlist.Name}.");
                }
            }

            return SkillResult.Text("Modo música activado. Si guardas una playlist preferida, también puedo ponerla automáticamente.");
        }

        var custom = await routines.Load(chatId, cancellationToken);

        if (custom.TryGetValue(routine, out string? description))
            return SkillResult.Text($"Rutina {routine} encontrada: {description}. Está guardada como rutina personalizada.");

        return SkillResult.Text("No reconocí esa rutina. Puedes decir: modo estudio, modo música o buenas noches.");
    }
}
