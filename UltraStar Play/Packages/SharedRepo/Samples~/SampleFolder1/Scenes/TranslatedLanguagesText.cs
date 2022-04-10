using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class TranslatedLanguagesText : MonoBehaviour
{
    private void Start()
    {
        Text uiText = GetComponent<Text>();
        if (TranslationManager.Instance == null)
        {
            uiText.text = "No TranslationManager";
            return;
        }
        
        List<string> translatedLanguages = TranslationManager.Instance.GetTranslatedLanguages()
            .Select(systemLanguage => LanguageHelper.Get2LetterIsoCodeFromSystemLanguage(systemLanguage))
            .ToList();
        uiText.text = "Translated languages: " + string.Join(", ", translatedLanguages);
    }
}
