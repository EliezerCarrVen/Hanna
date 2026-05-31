namespace Hanna.Models;

internal enum IntentType
{
    SystemCommand,
    GeneralChat,
    GeneralVerified,
    Weather,
    TrustedWebSearch,
    AudioControl,
    PersonalityModify,

    SpotifyDevices,
    SpotifyPlayTrack,
    SpotifyPlayAlbum,
    SpotifyQueueTrack,
    SpotifyQueueAlbum,
    SpotifyQueuePlaylist,
    SpotifyQueueList,
    SpotifyLikeTrack,
    SpotifyLikedList,
    SpotifyPause,
    SpotifyResume,
    SpotifyNext,
    SpotifyPrevious,
    SpotifyPlaylistCreate,
    SpotifyPlaylistAddTrack,
    SpotifyPlaylistPlay,
    SpotifyPlaylistList,

    YouTubeAudio,
    YouTubeVideo,
    Vision,
    AgentCode,

    OpenApp,
    OpenUrl,
    BrowserSearch,
    WebVideoDownload,

    FileList,
    FileRead,
    FileWrite,
    FileFind,

    ReminderSet,
    ReminderList,

    MemorySave,
    MemoryShow,
    PreferenceSet,
    PreferenceShow,

    RoutineRun,
    RoutineCreate,
    RoutineList,

    EngineModeChange,
    ConfigModify,
    CameraControl,
    Shutdown,

    SendMessage,
    ComputerSettingsInfo,

    DynamicSkill,
    AssignmentCreate,
    AssignmentList,
    AssignmentCheck,
    NotebookCreate,

    MediaNetflixPc,
    MediaNetflixTvLg,
    MediaYoutubeTvLg,
    TieredMemorySearch,
    PhaseControl
}
