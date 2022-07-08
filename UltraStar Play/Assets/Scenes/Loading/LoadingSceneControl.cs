using System.Collections;
using System.Collections.Generic;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LoadingSceneControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private Settings settings;

    void Start()
    {
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
        
        // Keep mobile devices from turning off the screen while the game is running.
        Screen.sleepTimeout = (int)0f;
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        // The settings are loaded on access.
        string jsonSettings = JsonConverter.ToJson(SettingsManager.Instance.Settings, false);
        Log.Logger.Information("loaded settings:" + jsonSettings);

        // The SongMetas are loaded on access.
        SongMetaManager.Instance.ScanFilesIfNotDoneYet();
        Log.Logger.Information("started loading songs.");

        // Extract StreamingAssets on Android from the JAR
        AndroidStreamingAssets.Extract();

        // Wait for Theme and I18N resources
        TranslationManager.Instance.ReloadTranslationsAndUpdateScene();

        // Ask for microphone permissions on Android
#if  UNITY_ANDROID
        AndroidRuntimePermissions.Permission checkPermission = AndroidRuntimePermissions.CheckPermission("android.permission.RECORD_AUDIO");
        if (checkPermission == AndroidRuntimePermissions.Permission.ShouldAsk)
        {
            AndroidRuntimePermissions.RequestPermission("android.permission.RECORD_AUDIO");
        }
#endif

        FinishScene();
    }

    private void FinishScene()
    {
        // Loading completed, continue with next scene
        SceneNavigator.Instance.LoadScene(EScene.MainScene);
    }

    private IEnumerator FinishAfterDelay()
    {
        // Wait delay in case loading just didn't finish yet.
        yield return new WaitForSeconds(2);
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
