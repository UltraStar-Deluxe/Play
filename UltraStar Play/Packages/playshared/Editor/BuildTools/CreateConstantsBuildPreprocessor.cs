using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class CreateConstantsBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log("CreateConstantsBuildPreprocessor - OnPreprocessBuild");
//        CreateConstantsMenuItems.CreateAllConstants();
    }
}
