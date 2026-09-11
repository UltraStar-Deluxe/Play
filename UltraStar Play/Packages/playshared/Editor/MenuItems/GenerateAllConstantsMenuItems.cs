using UnityEditor;
using PrimeInputActions;

public class GenerateAllConstantsMenuItems
{
    [MenuItem("Generate/C# Constants/All %&g")]
    public static void GenerateAllConstants()
    {
        EditorUtils.RefreshAssetsInStreamingAssetsFolder();

        GenerateUiConstantsMenuItems.GenerateConstantsForUxmlNamesAndUssClasses();
        GenerateTranslationConstantsMenuItems.GenerateTranslationConstants();
        GenerateInputActionConstantsMenuItem.GenerateInputActionConstants();
    }
}
