
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class Settings : ISettings
{
    // Graphics settings
    public ReactiveProperty<ScreenResolution> ScreenResolution { get; private set; } = new(new ScreenResolution(1280, 720, 60));
    public ReactiveProperty<FullScreenMode> FullScreenMode { get; private set; } = new(UnityEngine.FullScreenMode.Windowed);
    public ReactiveProperty<int> TargetFps { get; private set; } = new(30);

    // Audio settings
    public ReactiveProperty<int> PreviewVolumePercent { get; private set; } = new( 50);
    public ReactiveProperty<int> VolumePercent { get; private set; } = new(100);
    public ReactiveProperty<int> BackgroundMusicVolumePercent { get; private set; } = new( 50);
    public ReactiveProperty<int> VocalsAudioVolumePercent { get; private set; } = new(100);
    public ReactiveProperty<int> SceneChangeSoundVolumePercent { get; private set; } = new( 50);
    public ReactiveProperty<int> SfxVolumePercent { get; private set; } = new( 50);
    public ReactiveProperty<bool> PreferPortAudio { get; private set; } = new();
    public ReactiveProperty<bool> PlayRecordedAudio { get; private set; } = new();
    public ReactiveProperty<string> SoundfontPath { get; private set; } = new("");

    // Game settings
    public SystemLanguage Language { get; set; } = SystemLanguage.English;
    public EScoreMode ScoreMode { get; set; } = EScoreMode.Individual;
    public EDifficulty Difficulty { get; set; } = EDifficulty.Medium;
    public EPitchDetectionAlgorithm PitchDetectionAlgorithm { get; set; } = EPitchDetectionAlgorithm.Dywa;
    public string CommonScoreNameSeparator { get; set; } = " & ";
    public int defaultMedleyTargetDurationInSeconds = 30;
    public int reducedAudioVolumePercent = 1;
    public float passTheMicTimeInSeconds = 20;
    
    // Player profile settings
    public List<PlayerProfile> PlayerProfiles { get; set; } = new();
    
    // Recording device settings
    public List<MicProfile> MicProfiles { get; set; } = new();
    public string LastMicProfileNameInRecordingOptionsScene { get; set; }
    public int LastMicProfileChannelIndexInRecordingOptionsScene { get; set; }
    
    // Webcam settings
    public string CurrentWebcamDeviceName { get; set; }
    public bool UseWebcamAsBackgroundInSingScene { get; set; }

    // Song library settings
    public List<string> songDirs = new();
    public bool searchAudioFilesWithoutSongMeta;
    
    // Theme settings
    public string themeName = ThemeManager.DefaultThemeName;
    public bool disableDynamicThemes;
    // Screen.currentResolution may only be called from Start() and Awake(), thus use a dummy here.
    public bool animatedBackground = true;
    public int backgroundLightIndex = 1;
    
    // Design / presentation settings
    public ESceneChangeAnimation sceneChangeAnimation = ESceneChangeAnimation.Zoom;
    public float sceneChangeDurationInSeconds = 0.25f;
    public bool useImageAsCursor = true;
    public bool enableVfx = true;
    public bool showScrollBarInSongSelect;
    
    // Sing scene settings
    public ENoteDisplayMode noteDisplayMode = ENoteDisplayMode.SentenceBySentence;
    public bool showStaticLyrics = true;
    public bool showPitchIndicator;
    public bool showLyricsOnNotes;
    public bool showPlayerNames;
    public bool showScoreNumbers;
    public bool showSongProgress;
    public bool analyzeBeatsWithoutTargetNote = true;

    // Song select settings
    public ESongOrder songOrder = ESongOrder.Artist;
    public List<ESearchProperty> searchProperties = new()
    {
        ESearchProperty.Artist,
        ESearchProperty.Title,
    };

    // Technical settings
    public bool ShowFps { get; set; }
    public bool useUniversalCharsetDetector = true;

    /**
     * Require explicit user action to use custom event system
     * because of a Unity issue that can make the UI unusable on Android.
     * (see https://issuetracker.unity3d.com/issues/android-uitoolkit-buttons-cant-be-clicked-with-a-cursor-in-samsung-dex-when-using-eventsystem)
     */
    public bool enableEventSystemOnAndroid;

    // The releases to be ignored when checking for updates.
    // When containing the string "all", then all releases will be ignored.
    public List<string> IgnoredReleases { get; set; } = new();

    // Companion App / REST API settings
    public int UdpPortOnServer { get; set; } = 34567;
    public int UdpPortOnClient { get; set; } = 34568;
    public string OwnHost { get; set; }
    public Dictionary<string, List<HttpApiPermission>> HttpApiPermissions { get; set; } = new();
    
    // Other settings
    public PartyModeSettings PartyModeSettings { get; set; } = new();
    public SongEditorSettings SongEditorSettings { get; set; } = new();
}
