using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class SetLanguageDropdown : MonoBehaviour
{
    public void Start()
    {
        Dropdown dropdown = GetComponentInChildren<Dropdown>();
        
        // Fill dropdown with available languages.
        dropdown.options = TranslationManager.Instance.GetTranslatedLanguages()
            .Select(language => new Dropdown.OptionData(language.ToString()))
            .ToList();
        
        // Select current language in Dropdown.
        Dropdown.OptionData optionDataOfCurrentLanguage = dropdown.options
            .FirstOrDefault(option => option.text == TranslationManager.Instance.currentLanguage.ToString());
        dropdown.value = dropdown.options.IndexOf(optionDataOfCurrentLanguage);
        
        // Change TranslationManager language when dropdown value changes.
        dropdown.onValueChanged.AddListener(_ =>
        {
            SystemLanguage selectedLanguage = (SystemLanguage)Enum.Parse(typeof(SystemLanguage), dropdown.options[dropdown.value].text);
            TranslationManager.Instance.currentLanguage = selectedLanguage;
            TranslationManager.Instance.ClearCurrentLanguageTranslations();
            TranslationManager.Instance.UpdateTranslatorsInScene();
        });
    }
}
