using UnityEngine;
using UnityEngine.UI;

public class HeightOfInputFieldContentSizeFitter : MonoBehaviour
{
    private RectTransform rectTransform;
    private InputField inputField;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        inputField = GetComponentInChildren<InputField>();
    }

    void Update()
    {
        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, inputField.preferredHeight);
    }
}
