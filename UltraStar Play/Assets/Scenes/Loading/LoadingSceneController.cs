using System.Collections;
using UniInject;
using UnityEngine;

public class LoadingSceneController : MonoBehaviour
{
    void Start()
    {
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
        I18NManager.Instance.UpdateCurrentLanguageAndTranslations();
        ThemeManager.Instance.ReloadThemes();

        FinishScene();
    }

    void Update()
    {
        // The next scene should show up automatically.
        // However, in case of an Exception (e.g. song folder not found)
        // it might be useful to continue via button.
        if (Input.anyKeyDown)
        {
            StartCoroutine(FinishAfterDelay(2));
        }
    }

    private void FinishScene()
    {
        // Loading completed, continue with next scene
        SceneNavigator.Instance.LoadScene(EScene.MainScene);
    }

    private IEnumerator FinishAfterDelay(float delay)
    {
        // Wait delay in case loading just didn't finish yet.
        yield return new WaitForSeconds(delay);
        FinishScene();
    }
}
