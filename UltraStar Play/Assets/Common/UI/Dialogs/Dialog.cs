using System.Collections;
using System.Collections.Generic;
using ProTrans;
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
            TranslatedText i18NText = titleUiText.GetComponent<TranslatedText>();
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
}
