using UnityEditor;
using UnityEngine;

public class CreateConstantsOnLoad
{
    [InitializeOnLoadMethod]
    static void StaticInit()
    {
        Debug.Log("CreateConstantsOnLoad");
        // Don't create all constants. Otherwise it may lead to an endless loop of recompiling.
        CreateConstantsMenuItems.CreateConstantsForUxmlNamesAndUssClasses();
    }
}
