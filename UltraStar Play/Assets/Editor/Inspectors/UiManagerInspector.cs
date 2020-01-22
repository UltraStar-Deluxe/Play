using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UiManager))]
public class UiManagerInspector : EditorBase
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        UiManager uiManager = target as UiManager;

        if (GUILayout.Button("Destroy debug points"))
        {
            uiManager.DestroyAllDebugPoints();
        }
    }

}
