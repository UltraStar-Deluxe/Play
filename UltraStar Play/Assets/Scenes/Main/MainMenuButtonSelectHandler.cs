using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuButtonSelectHandler : MonoBehaviour, ISelectHandler
{
    public string buttonDescription;
    private MainMenuButtonDescription uiButtonDescriptionText;

    void Start()
    {
        uiButtonDescriptionText = FindObjectOfType<MainMenuButtonDescription>();
    }

    public void OnSelect(BaseEventData eventData)
    {
        uiButtonDescriptionText.SetText(buttonDescription);
    }
}
