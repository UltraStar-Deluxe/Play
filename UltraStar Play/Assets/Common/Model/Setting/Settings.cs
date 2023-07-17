
using System;
using System.Collections.Generic;
using Serilog.Events;
using UnityEngine;

[Serializable]
public class Settings : ISettings
{
    // Graphics settings
    public ScreenResolution ScreenResolution { get; set; } = new ScreenResolution(1280, 720, 60);
    public FullScreenMode FullScreenMode { get; set; } = FullScreenMode.Windowed;
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

    // Game settings
    public SystemLanguage Language { get; set; } = SystemLanguage.English;
    public EScoreMode ScoreMode { get; set; } = EScoreMode.Individual;
    public EDifficulty Difficulty { get; set; } = EDifficulty.Medium;
    public EPitchDetectionAlgorithm PitchDetectionAlgorithm { get; set; } = EPitchDetectionAlgorithm.Dywa;
    public string CommonScoreNameSeparator { get; set; } = new(" & ");
    public int DefaultMedleyTargetDurationInSeconds { get; set; } = 30;
    public int ReducedAudioVolumePercent { get; set; } = 1;
    public float PassTheMicTimeInSeconds { get; set; } = 20;
    
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
    public bool SearchAudioFilesWithoutSongMeta { get; set; }
    public string GeneratedFolderPath { get; set; } = "";
    
    // Theme settings
    public string ThemeName { get; set; } = ThemeManager.DefaultThemeName;
    public bool DisableDynamicThemes { get; set; }
    // Screen.currentResolution may only be called from Start() and Awake(), thus use a dummy here.
    public bool AnimatedBackground { get; set; } = true;
    public int BackgroundLightIndex { get; set; } = 4;
    
    // Design / presentation settings
    public ESceneChangeAnimation SceneChangeAnimation { get; set; } = ESceneChangeAnimation.Zoom;
    public float SceneChangeDurationInSeconds { get; set; } = 0.4f;
    public bool UseImageAsCursor { get; set; } = true;
    public bool EnableVfx { get; set; } = true;
    public bool ShowScrollBarInSongSelect { get; set; }
    public bool ShowSongIndexInSongSelect { get; set; }
    public ESongBackgroundScaleMode SongBackgroundScaleMode { get; set; } = ESongBackgroundScaleMode.FitOutside;
    
    // Sing scene settings
    public ENoteDisplayMode NoteDisplayMode { get; set; } = ENoteDisplayMode.SentenceBySentence;
    public bool ShowStaticLyrics { get; set; } = true;
    public bool ShowPitchIndicator { get; set; }
    public bool ShowLyricsOnNotes { get; set; }
    public bool ShowPlayerNames { get; set; } = true;
    public bool ShowScoreNumbers { get; set; } = true;
    public bool ShowSongProgress { get; set; } = true;
    public bool AnalyzeBeatsWithoutTargetNote { get; set; } = true;

    // Song select settings
    public ESongOrder SongOrder { get; set; } = ESongOrder.Artist;
    public List<ESearchProperty> SearchProperties { get; set; } = new()
    {
        ESearchProperty.Artist,
        ESearchProperty.Title,
    };

    // Technical settings
    public bool ShowFps { get; set; }
    public bool UseUniversalCharsetDetector { get; set; } = true;
    public bool StreamAudioInSingScene { get; set; } = true;
    public LogEventLevel MinimumLogLevel { get; set; }= LogEventLevel.Information;

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
    public int ConnectedClientMessageBufferTimeInMillis { get; set; } = 150;
    
    // WebView settings
    public List<string> AcceptedWebViewHosts { get; set; } = new();
    public bool DisableWebView { get; set; }
    
    // Other settings
    public PartyModeSettings PartyModeSettings { get; set; } = new();
    public SongEditorSettings SongEditorSettings { get; set; } = new();

    // Ffmpeg settings
    public bool UseFfmpegToPlayMediaFiles { get; set; }
    public Dictionary<string, string> FileFormatToFfmpegConversionArguments { get; set; } = new()
    {
        // Copy mkv codec and convert to mp4 (very fast)
        {"mkv", "-y -i \"INPUT_FILE\" -c copy \"INPUT_FILE_WITHOUT_EXTENSION.mp4\""},
        // Convert webm to webm with vp8
        {"webm", "-y -i \"INPUT_FILE\" -c:v libvpx -c:a libvorbis \"INPUT_FILE_WITHOUT_EXTENSION-vp8.webm\""},
        // Convert audio files to ogg
        {"ANY_AUDIO", "-y -i \"INPUT_FILE\" \"INPUT_FILE_WITHOUT_EXTENSION.ogg\""},
        // Convert audio files to webm with vp8
        {"ANY_VIDEO", "-y -i \"INPUT_FILE\" -c:v libvpx -c:a libvorbis \"INPUT_FILE_WITHOUT_EXTENSION.webm\""},
    };
    public bool LogFfmpegOutput { get; set; }
    public int MaxConcurrentSongMediaConversions { get; set; } = 3;

    /**
     * Check that VP9 is not used in webm files and AV1 is not used in mp4 files.
     * These codecs are not supported by Unity.
     * Note that these checks slow down the song search process significantly, thus disabled by default.
     */
    public bool CheckCodecIsSupported { get; set; }
}
