using Hanna.Core;
using Hanna.Models;
using Hanna.Services;
using Hanna.Spotify;
using Hanna.Utilities;
using Telegram.Bot;

namespace Hanna.Skills;

internal sealed class SpotifySkill : ISkill
{
    private readonly AppConfig config;
    private readonly SpotifyAuthService auth;
    private readonly SpotifySearchService search;
    private readonly SpotifyLibraryService library;
    private readonly SpotifyPlaybackService playback;
    private readonly ResponseService response;
    private readonly PreferencesService? preferences;
    private readonly SpotifySmartResolverService smartResolver;

    public SpotifySkill(
        AppConfig config,
        SpotifyAuthService auth,
        SpotifySearchService search,
        SpotifyLibraryService library,
        SpotifyPlaybackService playback,
        ResponseService response,
        PreferencesService? preferences = null,
        SpotifySmartResolverService? smartResolver = null)
    {
        this.config = config;
        this.auth = auth;
        this.search = search;
        this.library = library;
        this.playback = playback;
        this.response = response;
        this.preferences = preferences;
        this.smartResolver = smartResolver ?? new SpotifySmartResolverService(search, library);
    }

    public bool CanHandle(IntentResult intent)
    {
        return intent.Type is IntentType.SpotifyPlayTrack
            or IntentType.SpotifyQueueTrack
            or IntentType.SpotifyQueueAlbum
            or IntentType.SpotifyQueuePlaylist
            or IntentType.SpotifyQueueList
            or IntentType.SpotifyLikeTrack
            or IntentType.SpotifyLikedList
            or IntentType.SpotifyPause
            or IntentType.SpotifyResume
            or IntentType.SpotifyNext
            or IntentType.SpotifyPrevious
            or IntentType.SpotifyPlaylistCreate
            or IntentType.SpotifyPlaylistAddTrack
            or IntentType.SpotifyPlaylistPlay
            or IntentType.SpotifyPlaylistList;
    }

    public async Task<SkillResult> Handle(long chatId, string originalText, IntentResult intent, TelegramBotClient botClient, CancellationToken cancellationToken)
    {
        if (!auth.HasToken(chatId))
            return SkillResult.Text("Necesito que vincules Spotify primero. Escribe /auth.", true);

        return intent.Type switch
        {
            IntentType.SpotifyLikedList => SkillResult.Text(await library.ListLiked(chatId, intent.Limit <= 0 ? 10 : intent.Limit, cancellationToken)),
            IntentType.SpotifyLikeTrack => await LikeTrack(chatId, intent.Query, cancellationToken),
            IntentType.SpotifyPlayTrack => await PlaySmart(chatId, originalText, intent.RequestedDevice, cancellationToken),
            IntentType.SpotifyPlaylistPlay => await PlaySmart(chatId, originalText, intent.RequestedDevice, cancellationToken),
            IntentType.SpotifyQueueTrack => await QueueTrack(chatId, intent.Query, intent.RequestedDevice, cancellationToken),
            IntentType.SpotifyQueueAlbum => await QueueAlbum(chatId, originalText, intent.RequestedDevice, cancellationToken),
            IntentType.SpotifyQueuePlaylist => await QueuePlaylist(chatId, originalText, intent.RequestedDevice, cancellationToken),
            IntentType.SpotifyQueueList => SkillResult.Text(await playback.GetQueue(chatId, cancellationToken)),
            IntentType.SpotifyPause => await PlayerCommand(() => playback.Pause(chatId, cancellationToken), "Pausé Spotify."),
            IntentType.SpotifyResume => await PlayerCommand(() => playback.Resume(chatId, cancellationToken), "Continuando Spotify."),
            IntentType.SpotifyNext => await PlayerCommand(() => playback.Next(chatId, cancellationToken), "Siguiente canción."),
            IntentType.SpotifyPrevious => await PlayerCommand(() => playback.Previous(chatId, cancellationToken), "Canción anterior."),
            _ => SkillResult.NotHandled()
        };
    }

    private async Task<SkillResult> PlaySmart(long chatId, string originalText, string requestedDevice, CancellationToken cancellationToken)
    {
        string scold = TextTools.DramaticNameScold(originalText);
        string query = TextTools.ExtractSpotifyPlayQuery(originalText);

        if (string.IsNullOrWhiteSpace(query))
            return SkillResult.Text(scold + "Dime qué canción, álbum o playlist quieres reproducir en Spotify.");

        var match = await smartResolver.ResolveForPlay(chatId, originalText, cancellationToken);

        if (match == null)
            return SkillResult.Text(scold + "No encontré algo suficientemente cercano en tus playlists, Me gusta ni búsqueda pública de Spotify.");

        if (match.Score < 0.45)
        {
            string label = match.Type == SpotifySmartType.Track
                ? $"{match.Name} de {match.Artist}"
                : match.Name;
            return SkillResult.Text(scold + $"Encontré {label}, pero la coincidencia es baja ({match.Score:0.00}). Dime el nombre más claro o incluye artista.");
        }

        SpotifyOperationResult result;
        string okText;

        if (match.Type == SpotifySmartType.Playlist)
        {
            result = await playback.PlayPlaylist(chatId, match.Id, requestedDevice, cancellationToken);
            okText = match.Source == SpotifySmartSource.UserLibrary
                ? $"Reproduciendo tu playlist {match.Name} en Spotify."
                : $"Reproduciendo la playlist {match.Name} en Spotify.";
        }
        else if (match.Type == SpotifySmartType.Album)
        {
            result = await playback.PlayAlbum(chatId, match.Id, requestedDevice, cancellationToken);
            okText = $"Reproduciendo el álbum {match.Name} de {match.Artist} en Spotify.";
        }
        else
        {
            result = await playback.PlayTrack(chatId, match.Id, requestedDevice, cancellationToken);
            okText = $"Reproduciendo {match.Name} de {match.Artist} en Spotify.";
        }

        if (result.Success)
            return SkillResult.Text(scold + okText);

        return SpotifyError(result, "reproducir en Spotify", scold);
    }

    private async Task<SkillResult> LikeTrack(long chatId, string query, CancellationToken cancellationToken)
    {
        query = TextTools.NormalizeSpotifySpeech(query);

        if (string.IsNullOrWhiteSpace(query))
            return SkillResult.Text("Dime qué canción quieres guardar en tus Me gusta de Spotify.");

        var track = await search.SearchTrack(query, cancellationToken);

        if (track == null)
            return SkillResult.Text("No encontré esa canción en Spotify.");

        if (track.Score < 0.55)
            return SkillResult.Text($"Encontré {track.Name} de {track.Artist}, pero no estoy totalmente segura. Intenta decir el nombre y artista más claro.");

        bool already = await library.IsLiked(chatId, track.Id, cancellationToken);

        if (already)
            return SkillResult.Text($"{track.Name} de {track.Artist} ya estaba en tus Me gusta de Spotify.");

        var result = await library.AddToLiked(chatId, track.Id, cancellationToken);

        if (result.Success)
            return SkillResult.Text($"Listo. Guardé {track.Name} de {track.Artist} en tus Me gusta de Spotify.");

        return SkillResult.Text($"No pude guardar la canción. Código: {result.StatusCode}. Detalle: {result.Detail}");
    }

    private async Task<SkillResult> QueueTrack(long chatId, string query, string requestedDevice, CancellationToken cancellationToken)
    {
        query = TextTools.NormalizeSpotifySpeech(query);

        if (string.IsNullOrWhiteSpace(query))
            return SkillResult.Text("Dime qué canción quieres agregar a la fila.");

        var track = await search.SearchTrack(query, cancellationToken);

        if (track == null)
            return SkillResult.Text("No encontré esa canción en Spotify.");

        if (track.Score < 0.55)
            return SkillResult.Text($"Encontré {track.Name} de {track.Artist}, pero no estoy totalmente segura. Dime el nombre y artista más claro.");

        var result = await playback.AddToQueue(chatId, track.Id, requestedDevice, cancellationToken);

        if (result.Success)
            return SkillResult.Text($"Listo. Agregué {track.Name} de {track.Artist} a la fila.");

        return SpotifyError(result, "agregarla a la fila");
    }

    private async Task<SkillResult> QueueAlbum(long chatId, string originalText, string requestedDevice, CancellationToken cancellationToken)
    {
        string query = ExtractAlbumQuery(originalText);

        if (string.IsNullOrWhiteSpace(query))
            return SkillResult.Text("Dime qué álbum quieres agregar a la fila.");

        var album = await search.SearchAlbum(query, cancellationToken);

        if (album == null)
            return SkillResult.Text("No encontré ese álbum en Spotify.");

        if (album.Score < 0.45)
            return SkillResult.Text($"Encontré el álbum {album.Name} de {album.Artist}, pero no estoy totalmente segura. Dime el nombre más claro.");

        var result = await playback.AddAlbumToQueue(chatId, album.Id, requestedDevice, cancellationToken);

        if (result.Success)
            return SkillResult.Text($"Listo. Agregué el álbum {album.Name} de {album.Artist} a la fila. Canciones agregadas: {result.Detail}.");

        return SpotifyError(result, "agregar el álbum a la fila");
    }

    private async Task<SkillResult> QueuePlaylist(long chatId, string originalText, string requestedDevice, CancellationToken cancellationToken)
    {
        string query = ExtractPlaylistQueueQuery(originalText);

        if (string.IsNullOrWhiteSpace(query) && preferences != null)
            query = await preferences.Get(chatId, "spotify_playlist_preferida", cancellationToken);

        if (string.IsNullOrWhiteSpace(query))
            return SkillResult.Text("Dime qué playlist quieres agregar a la fila.");

        SpotifyPlaylistInfo? userPlaylist = null;

        try
        {
            userPlaylist = await library.FindPlaylist(chatId, query, cancellationToken);
        }
        catch
        {
        }

        if (userPlaylist != null)
        {
            var userResult = await playback.AddPlaylistToQueue(chatId, userPlaylist.Id, requestedDevice, cancellationToken);

            if (userResult.Success)
                return SkillResult.Text($"Listo. Agregué la playlist {userPlaylist.Name} a la fila. Canciones agregadas: {userResult.Detail}.");

            return SpotifyError(userResult, "agregar la playlist a la fila");
        }

        var publicPlaylist = await search.SearchPlaylist(query, cancellationToken);

        if (publicPlaylist == null)
            return SkillResult.Text("No encontré esa playlist en Spotify.");

        if (publicPlaylist.Score < 0.45)
            return SkillResult.Text($"Encontré la playlist {publicPlaylist.Name}, pero no estoy totalmente segura. Dime el nombre más claro.");

        var result = await playback.AddPlaylistToQueue(chatId, publicPlaylist.Id, requestedDevice, cancellationToken);

        if (result.Success)
            return SkillResult.Text($"Listo. Agregué la playlist {publicPlaylist.Name} a la fila. Canciones agregadas: {result.Detail}.");

        return SpotifyError(result, "agregar la playlist a la fila");
    }

    private static SkillResult SpotifyError(SpotifyOperationResult result, string action, string prefix = "")
    {
        if (result.StatusCode == 404 || result.Detail.Contains("NO_ACTIVE_DEVICE", StringComparison.OrdinalIgnoreCase))
            return SkillResult.Text(prefix + "No encontré un dispositivo activo de Spotify. Abre Spotify en tu PC o celular, espera unos segundos y vuelve a pedírmelo. Si sigue igual usa /dispositivos.");

        if (result.StatusCode == 403)
            return SkillResult.Text(prefix + "Spotify no permitió la acción. Revisa que tengas Premium y que hayas autorizado permisos con /spotify_reset y luego /auth.");

        return SkillResult.Text(prefix + $"No pude {action}. Código: {result.StatusCode}. Detalle: {result.Detail}");
    }

    private static async Task<SkillResult> PlayerCommand(Func<Task<SpotifyOperationResult>> action, string successText)
    {
        var result = await action();

        return SkillResult.Text(result.Success
            ? successText
            : $"No pude ejecutar el comando. Código: {result.StatusCode}. Detalle: {result.Detail}");
    }

    private static string ExtractAlbumQuery(string text)
    {
        string cleaned = TextTools.NormalizeSpotifySpeech(TextTools.RemoveWrongAssistantNames(text));
        cleaned = Regex.Replace(cleaned, @"\b(hanna|agrega|agregar|anade|añade|pon|mete|album|álbum|disco|a la fila|a la cola|en la fila|cola|fila|queue|spotify|por favor|despues|después)\b", " ", RegexOptions.IgnoreCase);
        return Regex.Replace(cleaned, @"\s+", " ").Trim();
    }

    private static string ExtractPlaylistQueueQuery(string text)
    {
        string cleaned = TextTools.NormalizeSpotifySpeech(TextTools.RemoveWrongAssistantNames(text));
        cleaned = Regex.Replace(cleaned, @"\b(hanna|agrega|agregar|anade|añade|pon|mete|playlist|play list|lista de reproducción|lista de reproduccion|a la fila|a la cola|en la fila|cola|fila|queue|spotify|por favor|despues|después)\b", " ", RegexOptions.IgnoreCase);
        return Regex.Replace(cleaned, @"\s+", " ").Trim();
    }
}
