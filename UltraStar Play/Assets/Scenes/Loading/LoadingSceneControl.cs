using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    private PlaylistManager playlistManager;

    [Inject]
    private SteamManager steamManager;

    [Inject]
    private SteamWorkshopManager steamWorkshopManager;

    [Inject(UxmlName = R.UxmlNames.unexpectedErrorLabel)]
    private Label unexpectedErrorLabel;

    [Inject(UxmlName = R.UxmlNames.unexpectedErrorContainer)]
    private VisualElement unexpectedErrorContainer;

    [Inject(UxmlName = R.UxmlNames.viewMoreButton)]
    private Button viewMoreButton;

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
        AwaitableUtils.ExecuteAfterDelayInSecondsAsync(0.5f, () =>
        {
            if (gameObject)
            {
                PreloadSongMedia();
            }
        });

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

    private void PreloadSongMedia()
    {
        // Get first few songs metas to cache them
        List<SongMeta> allSongMetas = songMetaManager.GetSongMetas().ToList();
        Debug.Log($"Preloading song media. Total found song metas so far: {allSongMetas.Count}, preloading up to {preloadSongCount} songs");
        List<SongMeta> songMetas = allSongMetas
            .Take(preloadSongCount)
            .ToList();
        songMetas.ForEach(songMeta => PreloadSongMetaMedia(songMeta));
    }

    private async void PreloadSongMetaMedia(SongMeta songMeta)
    {
        Debug.Log($"Preloading local media of song {songMeta}");
        try
        {
            // Preload audio
            if (SongMetaUtils.AudioResourceExists(songMeta)
                && !WebRequestUtils.IsHttpOrHttpsUri(SongMetaUtils.GetAudioUri(songMeta))
                && ApplicationUtils.IsSupportedAudioFormat(Path.GetExtension(SongMetaUtils.GetAudioUri(songMeta)))
                && !ApplicationUtils.IsSupportedMidiFormat(Path.GetExtension(SongMetaUtils.GetAudioUri(songMeta))))
            {
                string audioUri = SongMetaUtils.GetAudioUri(songMeta);
                AudioClip loadedAudioClip = await AudioManager.LoadAudioClipFromUriAsync(audioUri);
                Debug.Log($"Preloaded audio '{loadedAudioClip.name}'");
            }

            // Preload cover
            if (SongMetaUtils.CoverResourceExists(songMeta)
                && !WebRequestUtils.IsHttpOrHttpsUri(SongMetaUtils.GetCoverUri(songMeta))
                && ApplicationUtils.IsSupportedImageFormat(Path.GetExtension(SongMetaUtils.GetCoverUri(songMeta))))
            {
                string coverUri = SongMetaUtils.GetCoverUri(songMeta);
                await ImageManager.LoadSpriteFromUriAsync(coverUri);
                Debug.Log($"Preloaded cover image '{coverUri}'");
            }

            // Preload background
            if (SongMetaUtils.BackgroundResourceExists(songMeta)
                && !WebRequestUtils.IsHttpOrHttpsUri(SongMetaUtils.GetBackgroundUri(songMeta))
                && ApplicationUtils.IsSupportedImageFormat(Path.GetExtension(SongMetaUtils.GetBackgroundUri(songMeta))))
            {
                string backgroundUri = SongMetaUtils.GetBackgroundUri(songMeta);
                await ImageManager.LoadSpriteFromUriAsync(backgroundUri);
                Debug.Log($"Preloaded background image '{backgroundUri}'");
            }

            // Video resource of the song does not need to be cached.

            // Parse whole file by reading the voices.
            if (songMeta.TryGetVoice(EVoiceId.P1, out Voice _))
            {
                Debug.Log($"Preloaded voices of '{songMeta.GetArtistDashTitle()}'");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to preload media of song {songMeta}");
            Debug.LogException(ex);
        }
    }

    private void ShowGeneralErrorMessage()
    {
        Debug.LogWarning("Showing general error message in loading scene. Probably something went wrong.");
        unexpectedErrorContainer.ShowByDisplay();
        unexpectedErrorLabel.text = Translation.Get(R.Messages.loadingScene_unexpectedErrorMessage,
            "path", ApplicationUtils.ReplacePathsWithDisplayString(Log.logFilePath));
        viewMoreButton.text = Translation.Get(R.Messages.action_learnMore);
        viewMoreButton.RegisterCallbackButtonTriggered(_ => Application.OpenURL(Translation.Get(R.Messages.uri_logFiles)));
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
}
