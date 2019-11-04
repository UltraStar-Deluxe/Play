using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuButtonSelectHandler : MonoBehaviour, ISelectHandler
{
    public string buttonDescription;
    private ButtonDescription uiButtonDescriptionText;

    void Start()
    {
        uiButtonDescriptionText = FindObjectOfType<ButtonDescription>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        uiButtonDescriptionText.SetText(buttonDescription);
    }
}
