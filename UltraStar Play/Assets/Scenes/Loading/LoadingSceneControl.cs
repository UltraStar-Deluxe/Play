using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimeInputActions;
using Serilog.Events;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LoadingSceneControl : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public int preloadSongCount = 10;

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

    private void Start()
    {
        // Show general error message after short pause.
        // Normally, the next scene should start before the error message is shown.
        unexpectedErrorContainer.HideByDisplay();
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(8, () => ShowGeneralErrorMessage()));

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
            .Subscribe(_ => StartCoroutine(FinishAfterDelay()));
        InputManager.GetInputAction(R.InputActions.usplay_start).PerformedAsObservable()
            .Subscribe(_ => StartCoroutine(FinishAfterDelay()));
        InputManager.GetInputAction(R.InputActions.ui_click).PerformedAsObservable()
            .Subscribe(_ => StartCoroutine(FinishAfterDelay()));
        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable()
            .Subscribe(_ => StartCoroutine(FinishAfterDelay()));
        InputManager.GetInputAction(R.InputActions.usplay_enter).PerformedAsObservable()
            .Subscribe(_ => StartCoroutine(FinishAfterDelay()));
        hiddenContinueButton.RegisterCallbackButtonTriggered(_ => StartCoroutine(FinishAfterDelay()));

        // Keep mobile devices from turning off the screen while the game is running.
        Screen.sleepTimeout = (int)0f;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // Load playlists
        Debug.Log($"Preloading playlists");
        playlistManager.GetPlaylists(true, true);

        // The SongMetas are loaded on access.
        songMetaManager.ScanSongsIfNotDoneYet();
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(0.5f, () => PreloadSongMedia()));

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

        // Continue to next scene when preloading data has finished.
        long maxWaitTimeInMillis = 1200;
        long startTimeInMillis = TimeUtils.GetUnixTimeMilliseconds();
        StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
            () => IsAllPreloadingFinished || TimeUtils.IsDurationAboveThresholdInMillis(startTimeInMillis, maxWaitTimeInMillis),
            () => FinishScene()));
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

    private void PreloadSongMetaMedia(SongMeta songMeta)
    {
        Debug.Log($"Preloading local media of song {songMeta}");
        try
        {
            if (SongMetaUtils.AudioResourceExists(songMeta)
                && !WebRequestUtils.IsHttpOrHttpsUri(SongMetaUtils.GetAudioUri(songMeta))
                && ApplicationUtils.IsSupportedAudioFormat(Path.GetExtension(SongMetaUtils.GetAudioUri(songMeta)))
                && !ApplicationUtils.IsSupportedMidiFormat(Path.GetExtension(SongMetaUtils.GetAudioUri(songMeta))))
            {
                // Load as streaming audio
                AudioManager.LoadAudioClipFromUri(SongMetaUtils.GetAudioUri(songMeta)).Subscribe(
                    loadedAudioClip => Debug.Log($"Preloaded AudioClip {loadedAudioClip.name}"));
            }

            if (SongMetaUtils.CoverResourceExists(songMeta)
                && !WebRequestUtils.IsHttpOrHttpsUri(SongMetaUtils.GetCoverUri(songMeta))
                && ApplicationUtils.IsSupportedImageFormat(Path.GetExtension(SongMetaUtils.GetCoverUri(songMeta))))
            {
                ImageManager.LoadSpriteFromUri(SongMetaUtils.GetCoverUri(songMeta))
                    .Subscribe(_ => { });
            }

            if (SongMetaUtils.BackgroundResourceExists(songMeta)
                && !WebRequestUtils.IsHttpOrHttpsUri(SongMetaUtils.GetBackgroundUri(songMeta))
                && ApplicationUtils.IsSupportedImageFormat(Path.GetExtension(SongMetaUtils.GetBackgroundUri(songMeta))))
            {
                ImageManager.LoadSpriteFromUri(SongMetaUtils.GetBackgroundUri(songMeta))
                    .Subscribe(_ => { });
            }

            // Video resource of the song does not need to be cached.

            // Parse whole file by reading the voices.
            Voice voice = songMeta.Voices.FirstOrDefault();
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
            ClipboardUtils.CopyToClipboard(Log.GetLogHistoryAsText(LogEventLevel.Verbose));
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_copiedToClipboard));
        });
    }

    private void FinishScene()
    {
        // Loading completed, continue with next scene
        SceneNavigator.Instance.LoadScene(EScene.MainScene);
    }

    private IEnumerator FinishAfterDelay()
    {
        // Wait delay in case loading just didn't finish yet.
        yield return new WaitForSeconds(1);
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
