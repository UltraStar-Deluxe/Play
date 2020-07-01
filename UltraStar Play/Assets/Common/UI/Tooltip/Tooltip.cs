using UnityEngine;
using UnityEngine.UI;
using UniInject;
using System;

public class Tooltip : AbstractPointerSensitivePopup
{
    [InjectedInInspector]
    public Text uiText;

    // The padding must correspond to the value of the uiText's RectTransform
    public float paddingInPx = 12;

    public string Text
    {
        get
        {
            return uiText.text;
        }

        set
        {
            uiText.text = value;
            FitSizeToUiText();
        }
    }

    private void FitSizeToUiText()
    {
        RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, uiText.preferredWidth + paddingInPx);
        RectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, uiText.preferredHeight + paddingInPx);
    }
}
