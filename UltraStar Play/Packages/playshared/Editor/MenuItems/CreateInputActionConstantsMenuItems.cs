using UnityEditor;

public class CreateInputActionConstantsMenuItem
{
    [MenuItem("Generate/C# Constants/InputActions")]
    public static void CreateInputActionConstants()
    {
        PrimeInputActions.CreateInputActionConstantsMenuItems.CreateInputActionConstants();
    }
}
