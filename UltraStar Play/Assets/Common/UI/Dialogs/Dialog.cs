using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Dialog : MonoBehaviour
{
    public Text titleUiText;
    public Text messageUiText;

    public string Title
    {
        get
        {
            return titleUiText.text;
        }
        set
        {
            titleUiText.text = value;
            // Do not use default translation from I18NText
            I18NText i18NText = titleUiText.GetComponent<I18NText>();
            if (i18NText != null)
            {
                Destroy(i18NText);
            }
        }
    }

    public string Message
    {
        get
        {
            return messageUiText.text;
        }
        set
        {
            messageUiText.text = value;
        }
    }

    private void OnDestroy()
    {
        UiManager uiManager = UiManager.Instance;
        if (uiManager != null)
        {
            UiManager.Instance.OnDialogClosed(this);
        }
    }
}
