using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class EditorUtils
{
    public static List<T> GetSelectedComponents<T>()
    {
        List<T> result = new List<T>();

        GameObject[] activeGameObjects = Selection.gameObjects;
        if (activeGameObjects == null || activeGameObjects.Length == 0)
        {
            return result;
        }

        foreach (GameObject gameObject in activeGameObjects)
        {
            T rectTransform = gameObject.GetComponent<T>();
            if (rectTransform != null)
            {
                result.Add(rectTransform);
            }
        }
        return result;
    }

    public static void RefreshAssetsInResourcesFolder()
    {
        // Update Unity's version of files in the Resources folder
        AssetDatabase.ImportAsset("Assets/Resources", ImportAssetOptions.ImportRecursive);
    }

    public static void RefreshAssetsInStreamingAssetsFolder()
    {
        // Update Unity's version of files in the StreamingAssets folder
        AssetDatabase.ImportAsset("Assets/StreamingAssets", ImportAssetOptions.ImportRecursive);
    }
}
