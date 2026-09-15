using System;
using System.Collections;
using System.Collections.Generic;
using ProTrans;
using UnityEditor;
using UnityEngine;

public class PropertiesFileAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        string[][] pathArrays = { importedAssets, deletedAssets, movedAssets };
        if (IsDefaultPropertiesFileChanged(pathArrays)
            && DontDestroyOnLoadManager.Instance != null
            && TranslationManager.Instance.generateConstantsOnResourceChange)
        {
            Debug.Log("Generating translation constants because default properties file changed");
            GenerateTranslationConstantsMenuItems.GenerateTranslationConstants();
        }
    }

    private static bool IsDefaultPropertiesFileChanged(string[][] pathArrays)
    {
        foreach (string[] pathArray in pathArrays)
        {
            foreach (string path in pathArray)
            {
                if (path.EndsWith("messages.properties"))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
