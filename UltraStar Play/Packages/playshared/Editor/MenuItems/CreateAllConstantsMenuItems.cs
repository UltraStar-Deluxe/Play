using UnityEditor;
using PrimeInputActions;

public class CreateAllConstantsMenuItems
{
    [MenuItem("Generate/C# Constants/All %&g")]
    public static void CreateAllConstants()
    {
        EditorUtils.RefreshAssetsInStreamingAssetsFolder();

        CreateUiConstantsMenuItems.CreateConstantsForUxmlNamesAndUssClasses();
        CreateTranslationConstantsMenuItems.CreateTranslationConstants();
        CreateInputActionConstantsMenuItems.CreateInputActionConstants();
    }
}
