using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuButtonDescriptionSelectHandler : MonoBehaviour, ISelectHandler
{
    public string buttonDescription;
    private MenuButtonDescriptionText uiButtonDescriptionText;

    void Start()
    {
        uiButtonDescriptionText = FindObjectOfType<MenuButtonDescriptionText>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        uiButtonDescriptionText.SetText(buttonDescription);
    }
}
