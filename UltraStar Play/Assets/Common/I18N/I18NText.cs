using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

[RequireComponent(typeof(Text))]
[ExecuteInEditMode]
public class I18NText : MonoBehaviour
{
    [Delayed]
    public string key;
    private string lastKey;

    void Start()
    {
        UpdateTranslation();
    }

#if UNITY_EDITOR
    private void Update()
    {
        if (lastKey != key)
        {
            lastKey = key;
            UpdateTranslation();
        }
    }
#endif

    public void UpdateTranslation()
    {
        if (key == null)
        {
            Debug.LogWarning($"Missing translation key for object '{gameObject.name}'", gameObject);
            return;
        }

        string timmedKey = key.Trim();
        if (string.IsNullOrEmpty(timmedKey))
        {
            Debug.LogWarning($"Missing translation key for object '{gameObject.name}'", gameObject);
            return;
        }

        Dictionary<string, string> translationArguments = GetTranslationArguments();
        string translation = I18NManager.Instance.GetTranslation(timmedKey, translationArguments);
        Text uiText = GetComponent<Text>();
        uiText.text = translation;
    }

    protected virtual Dictionary<string, string> GetTranslationArguments()
    {
        return new Dictionary<string, string>();
    }
}
