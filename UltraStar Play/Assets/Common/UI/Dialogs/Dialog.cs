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
