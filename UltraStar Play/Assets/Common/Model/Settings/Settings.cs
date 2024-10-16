using System;
using System.Collections.Generic;
using CommonOnlineMultiplayer;

[Serializable]
public class Settings : ISettings
{
    // Graphics settings
    public ScreenResolution ScreenResolution { get; set; } = new ScreenResolution(1280, 720, 60);
    public EFullScreenMode FullScreenMode { get; set; } = EFullScreenMode.Windowed;
    public int TargetFps { get; set; } = -1;

    // Audio settings
    public int PreviewVolumePercent { get; set; } = 50;
    public int VolumePercent { get; set; } = 100;
    public int MusicVolumePercent { get; set; } = 100;
    public int MicrophonePlaybackVolumePercent { get; set; } = 100;
    public int BackgroundMusicVolumePercent { get; set; } = 50;
    public int VocalsAudioVolumePercent { get; set; } = 100;
    public int SceneChangeSoundVolumePercent { get; set; } = 30;
    public int SfxVolumePercent { get; set; } = 30;
    public bool PreferPortAudio { get; set; }
    public bool PlayRecordedAudio { get; set; }
    public string SoundfontPath { get; set; } = "";
    public float PreviewFadeInDurationInSeconds { get; set; } = 2;

    /**
     * Delay of the system audio backend until the samples are audible on the speaker.
     * Unity does not consider this in its time calculation. And the delay can be significant (audible) at least on Windows.
     * See also https://forum.unity.com/threads/use-wasapi-audio-backend-on-windows-for-low-latency-audio-output.1471044/
     *
     * Example:
     * - Unity returns a time position of 120ms in an AudioSource because these samples have been sent to the system.
     * - The samples have a delay until they are played on the speaker. This is the system audio backend delay.
     * - Assuming a system audio backend delay of 50ms in this example, the user hears the AudioSource at position 70ms.
     */
    public int SystemAudioBackendDelayInMillis { get; set; } =
#if UNITY_STANDALONE_WIN
        50
#else
        0
#endif
        ;

    // Game settings
    public string CultureInfoName { get; set; } = "en";
    public EScoreMode ScoreMode { get; set; } = EScoreMode.Individual;
    public EDifficulty Difficulty { get; set; } = EDifficulty.Medium;
    public EPitchDetectionAlgorithm PitchDetectionAlgorithm { get; set; } = EPitchDetectionAlgorithm.Dywa;
    public string CommonScoreNameSeparator { get; set; } = new(" & ");
    public int DefaultMedleyTargetDurationInSeconds { get; set; } = 30;
    public int ReducedAudioVolumePercent { get; set; } = 1;
    public float PassTheMicTimeInSeconds { get; set; } = 20;
    public bool JokerRuleEnabled { get; set; } = true;

    // Player profile settings
    public List<PlayerProfile> PlayerProfiles { get; set; } = new();

    // Recording device settings
    public List<MicProfile> MicProfiles { get; set; } = new();
    public string LastMicProfileNameInRecordingOptionsScene { get; set; } = "";
    public int LastMicProfileChannelIndexInRecordingOptionsScene { get; set; }

    // Webcam settings
    public string CurrentWebcamDeviceName { get; set; }
    public bool UseWebcamAsBackgroundInSingScene { get; set; }

    // Song library settings
    public List<string> SongDirs { get; set; } = new();
    public List<string> DisabledSongFolders { get; set; } = new();
    public bool SearchMidiFilesWithLyrics { get; set; }
    public string GeneratedFolderPath { get; set; } = "";
    public bool SaveVocalsAndInstrumentalAudioInFolderOfSong { get; set; }
    public EFetchType SongDataFetchType { get; set; } = EFetchType.Upfront;
    public int SongScanMaxBatchCount { get; set; } = 1;

    /**
     * The UltraStar song format version that is used to save a song when otherwise none is specified or unknown.
     */
    public EKnownUltraStarSongFormatVersion DefaultUltraStarSongFormatVersionForSave { get; set; } = EKnownUltraStarSongFormatVersion.V120;

    /**
     * The UltraStar song format version that is used to upgrade a song on save when it has a lower version.
     */
    public EUpgradeUltraStarSongFormatVersion UpgradeUltraStarSongFormatVersionForSave { get; set; } = EUpgradeUltraStarSongFormatVersion.None;

    // Theme settings
    public string ThemeName { get; set; } = ThemeManager.DefaultThemeName;
    public bool EnableDynamicThemes { get; set; } = true;
    public bool AnimatedBackground { get; set; } = true;
    public int BackgroundLightIndex { get; set; } = 4;

    // Design / presentation settings
    public ESceneChangeAnimation SceneChangeAnimation { get; set; } = ESceneChangeAnimation.Zoom;
    public float SceneChangeDurationInSeconds { get; set; } = 0.4f;
    public bool UseImageAsCursor { get; set; } = true;
    public bool EnableVfx { get; set; } = true;
    public bool ShowScrollBarInSongSelect { get; set; }
    public bool ShowSongIndexInSongSelect { get; set; }
    public bool NavigateByFoldersInSongSelect { get; set; }
    public ESongBackgroundScaleMode SongBackgroundScaleMode { get; set; } = ESongBackgroundScaleMode.FitOutside;

    // Sing scene settings
    public ENoteDisplayMode NoteDisplayMode { get; set; } = ENoteDisplayMode.SentenceBySentence;
    public int NoteDisplayLineCount { get; set; } = 10;
    public EStaticLyricsDisplayMode StaticLyricsDisplayMode { get; set; } = EStaticLyricsDisplayMode.Bottom;
    public bool WipeLyrics { get; set; } = true;
    public bool ShowPitchIndicator { get; set; }
    public bool ShowLyricsOnNotes { get; set; }
    public bool ShowPlayerInfoNextToNotes { get; set; }
    public bool ShowPlayerNames { get; set; } = true;
    public bool ShowScoreNumbers { get; set; } = true;
    public ESongProgressBar ShowSongProgressBar { get; set; } = ESongProgressBar.Plain;
    public bool AnalyzeBeatsWithoutTargetNote { get; set; } = true;

    // Song select settings
    public ESongOrder SongOrder { get; set; } = ESongOrder.Artist;
    public List<ESearchProperty> SearchProperties { get; set; } = new()
    {
        ESearchProperty.Artist,
        ESearchProperty.Title,
    };
    public Dictionary<string, MicProfileReference> PlayerProfileNameToLastUsedMicProfile { get; private set; } = new();
    public int SongPreviewDelayInMillis { get; set; } = 500;

    // Technical settings
    public bool ShowFps { get; set; }
    public bool UseUniversalCharsetDetector { get; set; } = true;
    public bool WriteUltraStarTxtFileWithByteOrderMark { get; set; }
    public ELogEventLevel MinimumLogLevel { get; set; } = ELogEventLevel.Information;

    /**
     * Require explicit user action to use custom event system
     * because of a Unity issue that can make the UI unusable on Android.
     * (see https://issuetracker.unity3d.com/issues/android-uitoolkit-buttons-cant-be-clicked-with-a-cursor-in-samsung-dex-when-using-eventsystem)
     */
    public bool EnableEventSystemOnAndroid { get; set; }

    // The releases to be ignored when checking for updates.
    // When containing the string "all", then all releases will be ignored.
    public List<string> IgnoredReleases { get; set; } = new();

    // Companion App / REST API settings
    public int ConnectionServerPort { get; set; } = 34567;
    public int HttpServerPort { get; set; } = 6789;
    public string HttpServerHost { get; set; } = new("");
    public Dictionary<string, List<HttpApiPermission>> HttpApiPermissions { get; set; } = new();
    public int CompanionClientMessageBufferTimeInMillis { get; set; } = 150;

    // Other settings
    public PartyModeSettings PartyModeSettings { get; set; } = new();
    public SongEditorSettings SongEditorSettings { get; set; } = new();

     // PortAudio settings
     public PortAudioHostApi PortAudioHostApi { get; set; } = PortAudioHostApi.Default;
     public string PortAudioOutputDeviceName { get; set; } = "";

    // Media format support settings
    public ESongVideoPlayback SongVideoPlayback { get; set; } = ESongVideoPlayback.AlwaysEnabled;

    // Vlc settings
    public bool LogVlcOutput { get; set; }
    public EThirdPartyLibraryUsage VlcToPlayMediaFilesUsage { get; set; } = EThirdPartyLibraryUsage.WhenUnsupportedByUnity;

    // Ffmpeg settings
    public bool LogFfmpegOutput { get; set; }
    public EThirdPartyLibraryUsage FfmpegToPlayMediaFilesUsage { get; set; } = EThirdPartyLibraryUsage.Never;
    public Dictionary<string, string> FileFormatToFfmpegConversionArguments { get; set; } = new()
    {
        // Copy mkv codec and convert to mp4 (very fast)
        {"mkv", "-y -i \"INPUT_FILE\" -c copy \"INPUT_FILE_WITHOUT_EXTENSION.mp4\""},
        // Convert audio files to ogg
        {"ANY_AUDIO", "-y -i \"INPUT_FILE\" \"INPUT_FILE_WITHOUT_EXTENSION.ogg\""},
        // Convert video files to mp4
        {"ANY_VIDEO", "-y -i \"INPUT_FILE\" -qscale 0 \"INPUT_FILE_WITHOUT_EXTENSION.mp4\""},
    };
    public int MaxConcurrentSongMediaConversions { get; set; } = 3;

    /**
     * Check that VP9 is not used in webm files and AV1 is not used in mp4 files.
     * These codecs are not supported by Unity.
     * Note that these checks slow down the song search process significantly, thus disabled by default.
     */
    public bool CheckCodecIsSupported { get; set; }

    // Mods
    public List<string> EnabledMods { get; private set; } = new();
    public bool ReloadModsOnFileChange { get; set; }

    // Online multiplayer
    public EOnlineMultiplayerBackend EOnlineMultiplayerBackend { get; set; } = EOnlineMultiplayerBackend.Steam;
    public string UnityTransportIpAddress { get; set; } = "127.0.0.1";
    public ushort UnityTransportPort { get; set; } = 7777;
    public ENetworkDelivery BeatAnalyzedEventNetworkDelivery { get; set; } = ENetworkDelivery.ReliableSequenced;
    public int OnlineMultiplayerSimulatedJitterInMillis { get; set; }
}
