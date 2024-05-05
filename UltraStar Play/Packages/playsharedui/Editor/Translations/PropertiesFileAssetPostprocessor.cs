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
        string currentLanguagePropertiesFileNameSuffix = PropertiesFileParser.GetLanguageAndRegionSuffix(TranslationConfig.Singleton.CurrentCultureInfo);
        string currentPropertiesFileName = $"messages{currentLanguagePropertiesFileNameSuffix}";
        bool propertiesFileChanged = false;

        string[][] pathArrays = { importedAssets, deletedAssets, movedAssets };
        foreach (string[] pathArray in pathArrays)
        {
            foreach (string path in pathArray)
            {
                if (path.EndsWith(currentPropertiesFileName))
                {
                    propertiesFileChanged = true;
                    Debug.Log("Reloading translations because of changed file: " + path);
                    break;
                }
            }
        }

        if (DontDestroyOnLoadManager.Instance != null
            && TranslationManager.Instance.generateConstantsOnResourceChange)
        {
            CreateTranslationConstantsMenuItems.CreateTranslationConstants();
        }
    }
}
