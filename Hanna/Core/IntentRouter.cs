using Hanna.Models;
using Hanna.Utilities;

namespace Hanna.Core;

internal sealed class IntentRouter
{
    public IntentResult Classify(string text)
    {
        string normalized = TextTools.Normalize(text);
        string musicQuery = TextTools.CleanMusicQuery(text);
        int limit = TextTools.ExtractNumber(text);
        string requestedDevice = TextTools.ExtractDevice(text);
        bool hasUrl = Regex.IsMatch(text, @"https?://\S+", RegexOptions.IgnoreCase);

        if (Regex.IsMatch(text.Trim(), @"^/\s*\S+", RegexOptions.IgnoreCase))
            return new IntentResult(IntentType.SystemCommand, text, limit, requestedDevice);

        if (IsMediaCapabilityQuestion(normalized) && !hasUrl)
            return new IntentResult(IntentType.GeneralChat, text, limit, requestedDevice);


        if (Regex.IsMatch(normalized, @"\b(baja|sube|pon|ajusta|cambia|mutea|silencia)\b") &&
            Regex.IsMatch(normalized, @"\b(volumen|spotify|chrome|discord|obs|audio)\b") &&
            Regex.IsMatch(normalized, @"\b(\d{1,3}|volumen|mutea|silencia)\b"))
            return new IntentResult(IntentType.AudioControl, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(personalidad|habla|tono|estilo|sé|se mas|se más|recuerda que quiero que|modifica tu forma|cambia tu forma)\b") &&
            Regex.IsMatch(normalized, @"\b(seria|serio|carismatica|carismática|natural|directa|directo|formal|casual|jarvis|humor|personalidad|hablar|responder)\b"))
            return new IntentResult(IntentType.PersonalityModify, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(noticias|actualidad|clima|temperatura|llover|llueve|hoy|mañana|manana)\b") &&
            !Regex.IsMatch(normalized, @"\b(reproduce|spotify|cancion|canción|playlist)\b"))
            return new IntentResult(IntentType.TrustedWebSearch, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(apagate|apagar hanna|cerrar hanna|salir|terminar programa|shutdown hanna)\b"))
            return new IntentResult(IntentType.Shutdown, "", limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(usa|cambia|activar|activa)\b") &&
            Regex.IsMatch(normalized, @"\b(groq|gemini|hibrido|híbrido|modelo|motor|openrouter|open router|open routes|openroutes)\b"))
            return new IntentResult(IntentType.EngineModeChange, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(fase|fases)\b") &&
            Regex.IsMatch(normalized, @"\b(local|offline|ahorro|programacion|programación|multimedia|ops|operaciones|estudio|nube|architect|arquitectura|actual|lista|disponibles)\b"))
            return new IntentResult(IntentType.PhaseControl, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(camara|cámara|webcam|led)\b") &&
            Regex.IsMatch(normalized, @"\b(enciende|encender|activa|activar|apaga|apagar|desactiva|desactivar|estado|toggle|indicador)\b"))
            return new IntentResult(IntentType.CameraControl, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(que recuerdas|qué recuerdas|muestra memoria|ver memoria|mi memoria)\b"))
            return new IntentResult(IntentType.MemoryShow, "", limit, requestedDevice);

        if (
            Regex.IsMatch(normalized, @"\b(recuerda|memoriza)\b") ||
            (
                Regex.IsMatch(normalized, @"\b(guarda|guardar|agrega|agregar)\b") &&
                Regex.IsMatch(normalized, @"\b(memoria|personalidad|contexto)\b")
            )
        )
        {
            return new IntentResult(IntentType.MemorySave, text, limit, requestedDevice);
        }

        if (Regex.IsMatch(normalized, @"\b(preferencia|preferencias|playlist preferida|playlist favorita|dispositivo preferido|dispositivo favorito)\b"))
            return Regex.IsMatch(normalized, @"\b(muestra|ver|cuales|cuáles|lista)\b")
                ? new IntentResult(IntentType.PreferenceShow, "", limit, requestedDevice)
                : new IntentResult(IntentType.PreferenceSet, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(rutina|modo estudio|modo musica|modo música|buenas noches|modo noche|modo concentracion|modo concentración)\b"))
        {
            if (Regex.IsMatch(normalized, @"\b(crea|crear|guarda|guardar|nueva)\b") && normalized.Contains("rutina"))
                return new IntentResult(IntentType.RoutineCreate, text, limit, requestedDevice);

            if (Regex.IsMatch(normalized, @"\b(lista|listar|muestra|ver|mis)\b") && normalized.Contains("rutina"))
                return new IntentResult(IntentType.RoutineList, "", limit, requestedDevice);

            return new IntentResult(IntentType.RoutineRun, text, limit, requestedDevice);
        }

        if (Regex.IsMatch(normalized, @"\b(lista recordatorios|mis recordatorios|ver recordatorios)\b"))
            return new IntentResult(IntentType.ReminderList, "", limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(recordatorio|recuerdame|recuérdame|alarma)\b"))
            return new IntentResult(IntentType.ReminderSet, text, limit, requestedDevice);

        bool webVideoAction =
            Regex.IsMatch(normalized, @"\b(extrae|extraer|descarga|descargar|baja|bajar|mandame|mándame|envia|envía)\b") &&
            Regex.IsMatch(normalized, @"\b(video|mp4|pagina|página|url|sitio|link|enlace)\b");

        if (hasUrl && webVideoAction)
            return new IntentResult(IntentType.WebVideoDownload, text, limit, requestedDevice);

        if (!hasUrl &&
            Regex.IsMatch(normalized, @"\b(extrae video|extraer video|descarga video|descargar video|baja video)\b") &&
            !IsMediaCapabilityQuestion(normalized))
        {
            return new IntentResult(IntentType.WebVideoDownload, text, limit, requestedDevice);
        }


        if (Regex.IsMatch(normalized, @"\b(tarea|entrega|actividad|assignment|homework|classroom|materia|cuaderno)\b"))
        {
            if (Regex.IsMatch(normalized, @"\b(lista|listar|muestra|ver|pendientes|proximas|próximas)\b"))
                return new IntentResult(IntentType.AssignmentList, text, limit, requestedDevice);

            if (Regex.IsMatch(normalized, @"\b(crea|crear|registra|agrega|agregar|nueva|nuevo)\b"))
                return new IntentResult(IntentType.AssignmentCreate, text, limit, requestedDevice);

            if (Regex.IsMatch(normalized, @"\b(revisa|verifica|actualiza|checa|classroom|profesor|profesores)\b"))
                return new IntentResult(IntentType.AssignmentCheck, text, limit, requestedDevice);

            if (Regex.IsMatch(normalized, @"\b(cuaderno|notebook|notas|estudiar|examen)\b"))
                return new IntentResult(IntentType.NotebookCreate, text, limit, requestedDevice);
        }

        if (normalized.Contains("spotify") && Regex.IsMatch(normalized, @"\b(reproduce|pon|play|toca|abre|abrir)\b"))
        {
            if (Regex.IsMatch(normalized, @"\b(album|álbum|disco)\b"))
                return new IntentResult(IntentType.SpotifyPlayAlbum, text, limit, requestedDevice);

            if (Regex.IsMatch(normalized, @"\b(playlist|play list|lista de reproduccion|lista de reproducción)\b"))
                return new IntentResult(IntentType.SpotifyPlaylistPlay, text, limit, requestedDevice);

            if (HasRealMediaQuery(musicQuery))
                return new IntentResult(IntentType.SpotifyPlayTrack, musicQuery, limit, requestedDevice);
        }

        if (Regex.IsMatch(normalized, @"\b(necesito|haz|hacer|genera|generame|genérame|programa|programame|programame|crea|escribe|dame)\b") &&
            Regex.IsMatch(normalized, @"\b(codigo|código|programa|script|sql|base de datos|clase|metodo|método|funcion|función|api|endpoint|consulta|query|visual studio|vscode|code)\b"))
            return new IntentResult(IntentType.AgentCode, text, limit, requestedDevice);

        // Hanna V6.1: multimedia local y memoria offline antes de caer a comandos genéricos.
        if (Regex.IsMatch(normalized, @"\b(memoria|recuerdas|recuerda|resumen diario|indice|índice|historial local|sin internet|offline)\b") &&
            Regex.IsMatch(normalized, @"\b(busca|buscar|consulta|consultar|que sabes|qué sabes|muestra|ver|dime)\b"))
            return new IntentResult(IntentType.TieredMemorySearch, text, limit <= 0 ? 5 : limit, requestedDevice);

        if (normalized.Contains("netflix") && Regex.IsMatch(normalized, @"\b(tv lg|tele lg|lg|television|televisión|pantalla)\b"))
            return new IntentResult(IntentType.MediaNetflixTvLg, text, limit, requestedDevice);

        if (normalized.Contains("youtube") && Regex.IsMatch(normalized, @"\b(tv lg|tele lg|lg|television|televisión|pantalla)\b"))
            return new IntentResult(IntentType.MediaYoutubeTvLg, text, limit, requestedDevice);

        if (normalized.Contains("netflix") && Regex.IsMatch(normalized, @"\b(abre|abrir|busca|buscar|reproduce|reproducir|pon|play|serie|pelicula|película)\b"))
            return new IntentResult(IntentType.MediaNetflixPc, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(abre|abrir|ejecuta|inicia)\b") &&
            Regex.IsMatch(normalized, @"\b(app|aplicacion|aplicación|programa|bloc|notepad|calculadora|chrome|edge|spotify|visual studio|vscode|cmd|powershell)\b"))
            return new IntentResult(IntentType.OpenApp, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(abre|abrir|entra|navega)\b") &&
            Regex.IsMatch(normalized, @"\b(web|pagina|página|url|sitio|youtube|google|github)\b"))
            return new IntentResult(IntentType.OpenUrl, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(busca en internet|busca en google|investiga en web|busqueda web|búsqueda web)\b"))
            return new IntentResult(IntentType.BrowserSearch, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(lista archivos|listar archivos|ver archivos|leer archivo|crear archivo|escribir archivo|buscar archivo)\b"))
        {
            if (Regex.IsMatch(normalized, @"\b(leer|abre archivo|mostrar archivo)\b"))
                return new IntentResult(IntentType.FileRead, text, limit, requestedDevice);

            if (Regex.IsMatch(normalized, @"\b(crear|escribir|guardar archivo)\b"))
                return new IntentResult(IntentType.FileWrite, text, limit, requestedDevice);

            if (Regex.IsMatch(normalized, @"\b(buscar|encontrar)\b"))
                return new IntentResult(IntentType.FileFind, text, limit, requestedDevice);

            return new IntentResult(IntentType.FileList, text, limit, requestedDevice);
        }

        if (Regex.IsMatch(normalized, @"\b(envia mensaje|manda mensaje|whatsapp|telegram)\b"))
            return new IntentResult(IntentType.SendMessage, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(volumen|brillo|wifi|modo oscuro|configuracion|configuración)\b"))
            return new IntentResult(IntentType.ComputerSettingsInfo, text, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(clima|temperatura|pronostico|pronóstico|tiempo)\b"))
        {
            string lugar = TextTools.ExtractWeatherPlace(text);
            return new IntentResult(IntentType.Weather, string.IsNullOrWhiteSpace(lugar) ? "Gómez Palacio, Durango, MX" : lugar, limit, requestedDevice);
        }

        if (Regex.IsMatch(normalized, @"\b(dispositivos|aparatos|equipos)\b") &&
            Regex.IsMatch(normalized, @"\b(spotify|vinculados|conectados|linea|línea)\b"))
            return new IntentResult(IntentType.SpotifyDevices, "", limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(pausa|pausar|deten|detener)\b") && normalized.Contains("spotify"))
            return new IntentResult(IntentType.SpotifyPause, "", limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(continua|reanuda|resume|sigue)\b") && normalized.Contains("spotify"))
            return new IntentResult(IntentType.SpotifyResume, "", limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(siguiente|next)\b") && normalized.Contains("spotify"))
            return new IntentResult(IntentType.SpotifyNext, "", limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(anterior|previous|regresa)\b") && normalized.Contains("spotify"))
            return new IntentResult(IntentType.SpotifyPrevious, "", limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(cola|fila|queue)\b") && Regex.IsMatch(normalized, @"\b(que hay|qué hay|ver|muestra|lista|listar)\b"))
            return new IntentResult(IntentType.SpotifyQueueList, "", limit, requestedDevice);
        if (Regex.IsMatch(normalized, @"\b(album|álbum|disco)\b") &&
        Regex.IsMatch(normalized, @"\b(agrega|agregar|anade|añade|pon|mete)\b") &&
        Regex.IsMatch(normalized, @"\b(cola|fila|queue|despues|después)\b"))
        {
            return new IntentResult(IntentType.SpotifyQueueAlbum, text, limit, requestedDevice);
        }

        if (Regex.IsMatch(normalized, @"\b(playlist|play list|lista de reproduccion|lista de reproducción)\b") &&
            Regex.IsMatch(normalized, @"\b(agrega|agregar|anade|añade|pon|mete)\b") &&
            Regex.IsMatch(normalized, @"\b(cola|fila|queue|despues|después)\b"))
        {
            return new IntentResult(IntentType.SpotifyQueuePlaylist, text, limit, requestedDevice);
        }
        if (Regex.IsMatch(normalized, @"\b(agrega|agregar|anade|añade|pon|mete)\b") &&
            Regex.IsMatch(normalized, @"\b(cola|fila|queue|despues|después)\b"))
            return new IntentResult(IntentType.SpotifyQueueTrack, musicQuery, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(playlist|play list|lista de reproduccion|lista de reproducción)\b"))
        {
            if (Regex.IsMatch(normalized, @"\b(crea|crear|nueva|nuevo)\b"))
                return new IntentResult(IntentType.SpotifyPlaylistCreate, text, limit, requestedDevice);

            if (Regex.IsMatch(normalized, @"\b(mis|muestra|ver|lista|listar|cuales|cuáles)\b"))
                return new IntentResult(IntentType.SpotifyPlaylistList, "", limit, requestedDevice);

            if (Regex.IsMatch(normalized, @"\b(agrega|agregar|anade|añade|guarda|guardar|mete)\b"))
                return new IntentResult(IntentType.SpotifyPlaylistAddTrack, text, limit, requestedDevice);

            if (Regex.IsMatch(normalized, @"\b(reproduce|pon|play|toca)\b"))
                return new IntentResult(IntentType.SpotifyPlaylistPlay, text, limit, requestedDevice);
        }

        if (Regex.IsMatch(normalized, @"\b(me gusta|favoritos|liked songs|biblioteca)\b") &&
            Regex.IsMatch(normalized, @"\b(lista|listar|dame|muestra|primeros|primeras|cuales|cuáles)\b"))
            return new IntentResult(IntentType.SpotifyLikedList, "", limit <= 0 ? 10 : limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(agrega|agregar|anade|añade|guardar|guarda|dale corazon|dale corazón)\b") &&
            Regex.IsMatch(normalized, @"\b(spotify|favoritos|me gusta|biblioteca)\b"))
            return new IntentResult(IntentType.SpotifyLikeTrack, musicQuery, limit, requestedDevice);

        if (Regex.IsMatch(normalized, @"\b(reproduce|pon|play|toca)\b") && normalized.Contains("spotify"))
            return new IntentResult(IntentType.SpotifyPlayTrack, musicQuery, limit, requestedDevice);

        bool explicitYouTube = Regex.IsMatch(normalized, @"\b(youtube|you tube)\b");
        bool wantsDownload = Regex.IsMatch(normalized, @"\b(descarga|descargar|baja|bajar|mp3|m4a|audio|video|mp4)\b");
        bool wantsPlay = Regex.IsMatch(normalized, @"\b(reproduce|pon|play|toca)\b");

        if (IsMediaCapabilityQuestion(normalized))
            return new IntentResult(IntentType.GeneralChat, text, limit, requestedDevice);

        if (wantsDownload && Regex.IsMatch(normalized, @"\b(video|mp4)\b") && HasRealMediaQuery(musicQuery))
            return new IntentResult(IntentType.YouTubeVideo, musicQuery, limit, requestedDevice);

        if (wantsDownload && HasRealMediaQuery(musicQuery))
            return new IntentResult(IntentType.YouTubeAudio, musicQuery, limit, requestedDevice);

        if (explicitYouTube && wantsPlay && HasRealMediaQuery(musicQuery))
            return new IntentResult(IntentType.YouTubeAudio, musicQuery, limit, requestedDevice);

        if (wantsPlay && HasRealMediaQuery(musicQuery))
            return new IntentResult(IntentType.SpotifyPlayTrack, musicQuery, limit, requestedDevice);

        if (NeedsWebVerification(normalized))
            return new IntentResult(IntentType.GeneralVerified, text, limit, requestedDevice);

        return new IntentResult(IntentType.GeneralChat, text, limit, requestedDevice);
    }

    private static bool IsMediaCapabilityQuestion(string normalized)
    {
        bool asks =
            Regex.IsMatch(normalized, @"\b(analiza|explica|dime|que tipo|que tipos|cuales|puedes|puedo|como|ayuda|informacion|información)\b");

        bool aboutMedia =
            Regex.IsMatch(normalized, @"\b(descargar|bajar|extraer|videos|video|audio|pagina|paginas|página|páginas|sitios|formatos)\b");

        bool directAction =
            Regex.IsMatch(normalized, @"\b(reproduce|pon|play|toca|descarga esta|descarga este|descarga el|descarga la|baja el|baja la|extrae el|extrae este|mandame el|mándame el)\b");

        return asks && aboutMedia && !directAction;
    }

    private static bool HasRealMediaQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return false;

        string normalized = TextTools.Normalize(query);

        if (normalized.Length < 3)
            return false;

        string[] blocked =
        {
            "que", "que tipo", "que tipos", "tipos", "puedes descargar", "puedes", "analiza",
            "dime", "explica", "informacion", "información", "videos", "video", "audio"
        };

        return !blocked.Any(b => normalized.Equals(TextTools.Normalize(b), StringComparison.OrdinalIgnoreCase) ||
                                 normalized.Contains(TextTools.Normalize(b) + " puedes"));
    }

    private static bool NeedsWebVerification(string normalized)
    {
        return Regex.IsMatch(normalized, @"\b(actual|actualmente|hoy|noticias|precio|precios|version|versión|lanzamiento|fecha|clima|quien es el presidente|quién es el presidente|ceo|director actual|ultimas|últimas|reciente|recientes)\b");
    }
}
