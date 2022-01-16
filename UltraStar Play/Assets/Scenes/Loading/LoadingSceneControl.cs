using System.Collections;
using UnityEngine;
using UniRx;
using PrimeInputActions;
using ProTrans; 

public class LoadingSceneControl : MonoBehaviour
{
    void Start()
    {
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
        ThemeManager.Instance.ReloadThemes();

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
}
