using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibVLCSharp;
using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LoadingSceneControl : MonoBehaviour, INeedInjection
{
    private const long MaxWaitTimeInMillis = 1200;

    [InjectedInInspector]
    public int preloadSongCount = 10;

    [InjectedInInspector]
    public TextAsset localVersionTextAsset;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private InGameDebugConsoleManager inGameDebugConsoleManager;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private SteamManager steamManager;

    [Inject]
    private SteamWorkshopManager steamWorkshopManager;

    [Inject(UxmlName = R.UxmlNames.unexpectedErrorLabel)]
    private Label unexpectedErrorLabel;

    [Inject(UxmlName = R.UxmlNames.unexpectedErrorContainer)]
    private VisualElement unexpectedErrorContainer;

    [Inject(UxmlName = R.UxmlNames.learnMoreButton)]
    private Button learnMoreButton;

    [Inject(UxmlName = R.UxmlNames.viewLogButton)]
    private Button viewLogButton;
    
    [Inject(UxmlName = R.UxmlNames.copyLogButton)]
    private Button copyLogButton;

    [Inject(UxmlName = R.UxmlNames.hiddenContinueButton)]
    private Button hiddenContinueButton;

    private bool IsAllPreloadingFinished => IsSteamWorkshopItemsDownloadFinished;
    private bool IsSteamWorkshopItemsDownloadFinished => steamWorkshopManager.DownloadState is SteamWorkshopManager.EDownloadState.Finished;

    private long waitStartTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();

    private bool hasFinishedScene;

    private void Start()
    {
        // Show general error message after short pause.
        // Normally, the next scene should start before the error message is shown.
        unexpectedErrorContainer.HideByDisplay();
        AwaitableUtils.ExecuteAfterDelayInSecondsAsync(8, () =>
        {
            if (gameObject)
            {
                ShowGeneralErrorMessage();
            }
        });

        // Log version info
        Debug.Log($"VERSION.txt file content:\n{localVersionTextAsset.text}");

        // The settings are loaded on access.
        Settings settings = SettingsManager.Instance.Settings;
        string jsonSettings = JsonConverter.ToJson(settings, false);
        Debug.Log("loaded settings:" + jsonSettings);

        // Init song folders if none yet
        if (settings.SongDirs.IsNullOrEmpty())
        {
            settings.SongDirs = CreateInitialSongFolders();
        }
        
        // Create custom player profile images folder
        DirectoryUtils.CreateDirectory(PlayerProfileUtils.GetDefaultPlayerProfileImageFolderAbsolutePath());

        // The next scene should show up automatically.
        // However, in case of an Exception (e.g. song folder not found)
        // it might be useful to continue via button.
        InputManager.GetInputAction(R.InputActions.ui_submit).PerformedAsObservable()
            .Subscribe(_ => FinishAfterDelay());
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => FinishAfterDelay());
        InputManager.GetInputAction(R.InputActions.ui_click).PerformedAsObservable()
            .Subscribe(_ => FinishAfterDelay());
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => FinishAfterDelay());
        InputManager.GetInputAction(R.InputActions.usplay_enter).PerformedAsObservable()
            .Subscribe(_ => FinishAfterDelay());
        hiddenContinueButton.RegisterCallbackButtonTriggered(_ => FinishAfterDelay());

        // Keep mobile devices from turning off the screen while the game is running.
        Screen.sleepTimeout = (int)0f;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Load playlists
        Debug.Log($"Preloading playlists");
        playlistManager.GetPlaylists(true, true);

        // The SongMetas are loaded on access.
        songMetaManager.ScanSongsIfNotDoneYet();

        // Extract StreamingAssets on Android from the JAR
        AndroidStreamingAssets.Extract();

        // Ask for microphone and webcam permissions on Android
        if (PlatformUtils.IsAndroid)
        {
            AndroidRuntimePermissions.Permission checkAudioPermission = AndroidRuntimePermissions.CheckPermission("android.permission.RECORD_AUDIO");
            if (checkAudioPermission == AndroidRuntimePermissions.Permission.ShouldAsk)
            {
                AndroidRuntimePermissions.RequestPermission("android.permission.RECORD_AUDIO");
            }

            AndroidRuntimePermissions.Permission checkCameraPermission = AndroidRuntimePermissions.CheckPermission("android.permission.CAMERA");
            if (checkCameraPermission == AndroidRuntimePermissions.Permission.ShouldAsk)
            {
                AndroidRuntimePermissions.RequestPermission("android.permission.CAMERA");
            }
        }

        MidiManager.Instance.InitIfNotDoneYet();

        Debug.Log("Supported file extensions by vlc: " + ApplicationUtils.vlcSupportedFileExtensions.JoinWith(", "));

        PreloadLibVlc();

        waitStartTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
    }

    private void Update()
    {
        // Continue to next scene when preloading data has finished, or max wait time has been reached.
        if (IsAllPreloadingFinished
            || TimeUtils.IsDurationAboveThresholdInMillis(waitStartTimeInMillis, MaxWaitTimeInMillis))
        {
            FinishScene();
        }
    }

    private void ShowGeneralErrorMessage()
    {
        Debug.LogWarning("Showing general error message in loading scene. Probably something went wrong.");
        unexpectedErrorContainer.ShowByDisplay();
        unexpectedErrorLabel.text = Translation.Get(R.Messages.loadingScene_unexpectedErrorMessage,
            "path", ApplicationUtils.ReplacePathsWithDisplayString(Log.logFilePath));
        learnMoreButton.text = Translation.Get(R.Messages.action_learnMore);
        learnMoreButton.RegisterCallbackButtonTriggered(_ => Application.OpenURL(Translation.Get(R.Messages.uri_logFiles)));
        viewLogButton.RegisterCallbackButtonTriggered(_ => inGameDebugConsoleManager.ShowConsole());
        copyLogButton.RegisterCallbackButtonTriggered(_ =>
        {
            ClipboardUtils.CopyToClipboard(Log.GetLogHistoryAsText(ELogEventLevel.Verbose));
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_copiedToClipboard));
        });
    }

    private void FinishScene()
    {
        if (hasFinishedScene)
        {
            return;
        }
        hasFinishedScene = true;

        // Loading completed, continue with next scene
        SceneNavigator.Instance.LoadScene(EScene.MainScene);
    }

    private async void FinishAfterDelay()
    {
        // Wait delay in case loading just didn't finish yet.
        await Awaitable.WaitForSecondsAsync(1);
        FinishScene();
    }

    private List<string> CreateInitialSongFolders()
    {
        // Must be called from main thread because of Application.persistentDataPath
        List<string> result = new();
#if UNITY_ANDROID
        string internalStoragePath = AndroidUtils.GetAppSpecificStorageAbsolutePath(false);
        result.Add(internalStoragePath + "/Songs");
        string sdCardStoragePath = AndroidUtils.GetAppSpecificStorageAbsolutePath(true);
        result.Add(sdCardStoragePath + "/Songs");
#else
        result.Add(Application.persistentDataPath + "/Songs");
#endif
        return result;
    }

    /**
     * Preload libVLC to avoid lag in the game.
     */
    private void PreloadLibVlc()
    {
        try
        {
            MediaPlayer mediaPlayer = VlcManager.Instance.CreateMediaPlayer();
            Debug.Log($"Preloaded libVLC instance, mediaPlayer: {mediaPlayer}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to preload libVLC instance: {e.Message}");
            Debug.LogException(e);
        }
    }
}
