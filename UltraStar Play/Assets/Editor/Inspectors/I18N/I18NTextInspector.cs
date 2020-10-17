using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System;

[CustomEditor(typeof(I18NText), true)]
public class I18NTextInspector : EditorBase
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        I18NText i18nText = target as I18NText;

        // Update translations button
        if (GUILayout.Button("Update Translation"))
        {
            i18nText.UpdateTranslation();
        }
    }
}
