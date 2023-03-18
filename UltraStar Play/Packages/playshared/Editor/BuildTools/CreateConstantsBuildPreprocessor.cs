using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

public class CreateConstantsBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("CreateConstantsBuildPreprocessor - OnPreprocessBuild");
        CreateConstantsMenuItems.CreateAllConstants();
    }
}
