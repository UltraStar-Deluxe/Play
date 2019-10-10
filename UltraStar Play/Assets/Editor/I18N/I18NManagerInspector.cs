using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(I18NManager))]
public class I18NManagerInspector : EditorBase
{

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Update All Translations"))
        {
            I18NText[] i18nTexts = GameObject.FindObjectsOfType<I18NText>();
            Debug.Log($"I18NText instances in scene: {i18nTexts.Length}");
            foreach (I18NText i18nText in i18nTexts)
            {
                i18nText.UpdateTranslation();
                EditorUtility.SetDirty(i18nText.gameObject);
            }
        }
    }

}
