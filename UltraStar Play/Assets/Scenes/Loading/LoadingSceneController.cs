using UnityEngine;

public class LoadingSceneController : MonoBehaviour
{

    void Start()
    {
        // The settings are loaded on access.
        string jsonSettings = JsonConverter.ToJson(SettingsManager.Instance.Settings, true);
        Debug.Log("loaded settings:" + jsonSettings);

        // The SongMetas are loaded on access.
        Debug.Log("loaded songs: " + SongMetaManager.Instance.SongMetas.Count);

        // Loading completed, continue with next scene
        FinishScene();
    }

    void Update()
    {
        // The next scene should show up automatically.
        // However, in case of an Exception (e.g. song folder not found)
        // it might be useful to continue via button.
        if (Input.anyKeyDown)
        {
            FinishScene();
        }
    }

    private void FinishScene()
    {
        SceneNavigator.Instance.LoadScene(EScene.MainScene);
    }

}
