using ProTrans;
using UnityEditor;
using UnityEngine;

public class ReloadSceneWithClearCacheMenuItem
{
    [MenuItem("Tools/Reload Scene with Clear Cache &r")]
    public static void ReloadSceneWithClearCache()
    {
        RefreshAssetDatabase();
        ClearTranslationCache();
        ReloadCurrentScene();
    }

    private static void RefreshAssetDatabase()
    {
        AssetDatabase.Refresh();
    }

    private static void ClearTranslationCache()
    {
        (TranslationConfig.Singleton.PropertiesFileProvider as CachingPropertiesFileProvider)?.ClearCache();
    }

    private static void ReloadCurrentScene()
    {
        EScene currentScene = SceneRecipeManager.Instance.GetCurrentScene();
        Debug.Log($"Reloading scene: {currentScene}");
        SceneNavigator.Instance.LoadScene(currentScene);
    }
}
