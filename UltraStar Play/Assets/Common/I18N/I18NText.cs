using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

[RequireComponent(typeof(Text))]
public class I18NText : MonoBehaviour
{
    [ReadOnly]
    public string key;

    void Start()
    {
        UpdateTranslation();
    }

    public void UpdateTranslation()
    {
        string timmedKey = key.Trim();
        if (string.IsNullOrEmpty(timmedKey))
        {
            Debug.LogWarning($"Missing translation key for object '{gameObject.name}'", gameObject);
            return;
        }

        Text text = GetComponent<Text>();
        Dictionary<string, string> translationArguments = GetTranslationArguments();
        string translation = I18NManager.Instance.GetTranslation(timmedKey, translationArguments);
        text.text = translation;
    }

    protected virtual Dictionary<string, string> GetTranslationArguments()
    {
        return new Dictionary<string, string>();
    }
}
