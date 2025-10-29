using PrimeInputActions;
using UnityEditor;
using UnityEngine;

public class InputActionAssetPostprocessor : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        InputManager inputManager = InputManager.Instance;
        if (inputManager == null
            || !inputManager.generateConstantsOnResourceChange
            || inputManager.defaultInputActionAsset == null)
        {
            return;
        }

        string defaultInputActionAssetPath = AssetDatabase.GetAssetPath(inputManager.defaultInputActionAsset);
        
        string[][] pathArrays = { importedAssets, deletedAssets, movedAssets };
        foreach (string[] pathArray in pathArrays)
        {
            foreach (string path in pathArray)
            {
                if (path.EndsWith(defaultInputActionAssetPath))
                {
                    if (inputManager.LogInfoNow)
                    {
                        Debug.Log("Creating InputAction path constants because of changed file: " + path);
                    }
                    CreateInputActionConstantsMenuItem.CreateInputActionConstants();
                    return;
                }
            }
        }
    }
}
