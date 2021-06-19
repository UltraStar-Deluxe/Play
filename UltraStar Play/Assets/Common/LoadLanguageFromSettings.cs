using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LoadLanguageFromSettings : MonoBehaviour, INeedInjection
{
    [Inject]
    private Settings settings;

    [Inject]
    private TranslationManager translationManager;
    
    private void Start()
    {
        if (translationManager.currentLanguage != settings.GameSettings.language)
        {
            translationManager.currentLanguage = settings.GameSettings.language;
            translationManager.ReloadTranslationsAndUpdateScene();
        }
    }
}
