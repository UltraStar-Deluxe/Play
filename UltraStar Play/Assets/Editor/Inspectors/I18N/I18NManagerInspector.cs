using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(I18NManager))]
public class I18NManagerInspector : EditorBase
{
    public override void OnInspectorGUI()
    {
        I18NManager i18nManager = target as I18NManager;
        bool lastIsOverwriteLanguage = i18nManager.isOverwriteLanguage;
        SystemLanguage lastOverwriteLanguage = i18nManager.overwriteLanguage;

        DrawDefaultInspector();

        if (GUILayout.Button("Update Translations in Scene"))
        {
            UpdateAllTranslations();
        }

        if (GUILayout.Button("Reload Translations")
            || lastIsOverwriteLanguage != i18nManager.isOverwriteLanguage
            || lastOverwriteLanguage != i18nManager.overwriteLanguage)
        {

            if (i18nManager.updateAllTranslationsWhenReloadingLangauge)
            {
                i18nManager.UpdateCurrentLanguageAndTranslations(() => UpdateAllTranslations());
            }
            else
            {
                i18nManager.UpdateCurrentLanguageAndTranslations();
            }
        }
    }

    private void UpdateAllTranslations()
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
