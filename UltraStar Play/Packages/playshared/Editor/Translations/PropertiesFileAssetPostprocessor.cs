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
        TranslationManager translationManager = TranslationManager.Instance;
        if (translationManager == null)
        {
            return;
        }

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

        if (propertiesFileChanged)
        {
            TranslationManager.ReloadTranslationsAndUpdateScene();
        }

        if (translationManager.generateConstantsOnResourceChange)
        {
            CreateTranslationConstantsMenuItems.CreateTranslationConstants();
        }
    }
}
