using UnityEngine;
using UnityEngine.UI;

public class TextFieldContentSizeFitter : MonoBehaviour
{
    public Text uiText;

    public bool setHeight = true;
    public bool setWidth;

    private RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (setHeight)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, uiText.preferredHeight);
        }
        if (setWidth)
        {
            rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, uiText.preferredWidth);
        }
    }
}
