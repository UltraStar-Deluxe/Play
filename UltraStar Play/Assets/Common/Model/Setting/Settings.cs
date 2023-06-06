
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class Settings : ISettings
{
    // Graphics settings
    public ScreenResolution ScreenResolution { get; set; } = new ScreenResolution(1280, 720, 60);
    public FullScreenMode FullScreenMode { get; set; } = UnityEngine.FullScreenMode.Windowed;
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
    public bool SearchAudioFilesWithoutSongMeta { get; set; }
    
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
    public bool ShowPlayerNames { get; set; }
    public bool ShowScoreNumbers { get; set; }
    public bool ShowSongProgress { get; set; }
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
    public int UdpPortOnServer { get; set; } = 34567;
    public int UdpPortOnClient { get; set; } = 34568;
    public string OwnHost { get; set; } = new("");
    public Dictionary<string, List<HttpApiPermission>> HttpApiPermissions { get; set; } = new();
    public int ConnectedClientMessageBufferTimeInMillis { get; set; } = 200;
    
    // Other settings
    public PartyModeSettings PartyModeSettings { get; set; } = new();
    public SongEditorSettings SongEditorSettings { get; set; } = new();
}
