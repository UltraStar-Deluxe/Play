using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System;

[CustomEditor(typeof(I18NText), true)]
public class I18NTextInspector : EditorBase
{

    // Fields for implementing the key drop-down
    private SerializedProperty keyProp;
    private int i18nKeyIndex;

    private string[] i18nKeys;

    void OnEnable()
    {
        i18nKeys = I18NManager.Instance.GetKeys().ToArray();

        // Setup the SerializedProperties.
        keyProp = serializedObject.FindProperty("key");
        // Set the choice index to the previously selected index
        i18nKeyIndex = Array.IndexOf(i18nKeys, keyProp.stringValue);
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        I18NText myScript = target as I18NText;

        // Change i18nKey via drop-down
        int lastIndex = i18nKeyIndex;
        i18nKeyIndex = EditorGUILayout.Popup("Key", i18nKeyIndex, i18nKeys);
        if (i18nKeyIndex < 0)
        {
            i18nKeyIndex = 0;
        }
        if (lastIndex != i18nKeyIndex)
        {
            keyProp.stringValue = i18nKeys[i18nKeyIndex];
            serializedObject.ApplyModifiedProperties();
            myScript.UpdateTranslation();
        }

        // Update translations button
        if (GUILayout.Button("Update Translation"))
        {
            myScript.UpdateTranslation();
        }
    }
}
