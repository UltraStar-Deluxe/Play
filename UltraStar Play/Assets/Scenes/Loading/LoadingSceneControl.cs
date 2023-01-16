using System;
using System.Collections;
using System.Collections.Generic;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LoadingSceneControl : MonoBehaviour, INeedInjection
{
    [Inject(UxmlName = R.UxmlNames.unexpectedErrorLabel)]
    private Label unexpectedErrorLabel;

    [Inject(UxmlName = R.UxmlNames.hiddenContinueButton)]
    private Button hiddenContinueButton;

    private void Awake()
    {
        // Show general error message after short pause.
        // Normally, the next scene should start before the error message is shown.
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(10, () => ShowGeneralErrorMessage()));
    }

    private void Start()
    {
        // The settings are loaded on access.
        Settings settings = SettingsManager.Instance.Settings;
        string jsonSettings = JsonConverter.ToJson(settings, false);
        Log.Logger.Information("loaded settings:" + jsonSettings);

        // Init song folders if none yet
        if (settings.GameSettings.songDirs.IsNullOrEmpty())
        {
            settings.GameSettings.songDirs = CreateInitialSongFolders();
        }

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
        hiddenContinueButton.RegisterCallbackButtonTriggered(() => StartCoroutine(FinishAfterDelay()));
        
        // Keep mobile devices from turning off the screen while the game is running.
        Screen.sleepTimeout = (int)0f;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;

        // The SongMetas are loaded on access.
        SongMetaManager.Instance.ScanFilesIfNotDoneYet();
        Log.Logger.Information("started loading songs.");

        // Extract StreamingAssets on Android from the JAR
        AndroidStreamingAssets.Extract();

        // Wait for Theme and I18N resources
        TranslationManager.Instance.ReloadTranslationsAndUpdateScene();

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

        FinishScene();
    }

    private void ShowGeneralErrorMessage()
    {
        Debug.LogWarning("Showing general error message in loading scene. Probably something went wrong.");
        unexpectedErrorLabel.ShowByDisplay();
        unexpectedErrorLabel.text = TranslationManager.GetTranslation(R.Messages.loadingScene_unexpectedErrorMessage,
            "path", Log.logFileFolder);
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
