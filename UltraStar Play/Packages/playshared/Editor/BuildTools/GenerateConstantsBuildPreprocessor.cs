using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class GenerateConstantsBuildPreprocessor : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        Debug.Log($"{nameof(GenerateConstantsBuildPreprocessor)} - OnPreprocessBuild");
//        GenerateConstantsMenuItems.CreateAllConstants();
    }
}
