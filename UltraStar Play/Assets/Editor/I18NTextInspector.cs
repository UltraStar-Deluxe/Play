using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

[CustomEditor(typeof(I18NText), true)]
public class I18NTextInspector : EditorBase
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        I18NText myScript = target as I18NText;
        if (GUILayout.Button("Update Translation"))
        {
            myScript.UpdateTranslation();
        }
    }
}
