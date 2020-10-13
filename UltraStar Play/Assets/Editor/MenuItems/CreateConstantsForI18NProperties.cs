using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class CreateConstantsForI18NProperties
{
    [MenuItem("Resources/Create C# constants for I18N properties")]
    public static void CreateI18nConstants()
    {
        string subClassName = "String";
        string targetPath = $"Assets/Common/R/{CreateConstantsUtils.className + subClassName}.cs";

        List<string> i18nKeys = I18NManager.Instance.GetKeys();
        if (i18nKeys.IsNullOrEmpty())
        {
            Debug.LogWarning("No i18n keys found.");
            return;
        }

        i18nKeys.Sort();
        string classCode = CreateConstantsUtils.CreateClassCode(subClassName, i18nKeys);
        File.WriteAllText(targetPath, classCode);
        Debug.Log("Generated file " + targetPath);
    }
}
