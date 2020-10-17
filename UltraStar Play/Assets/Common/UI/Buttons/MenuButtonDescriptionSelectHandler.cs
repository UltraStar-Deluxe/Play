using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MenuButtonDescriptionSelectHandler : MonoBehaviour, ISelectHandler, INeedInjection
{
    public string buttonDescriptionI18nKey;

    [Inject]
    private MenuButtonDescriptionText uiButtonDescriptionText;

    [Inject]
    private I18NManager i18nManager;

    public void OnSelect(BaseEventData eventData)
    {
        if (uiButtonDescriptionText == null)
        {
            return;
        }

        string description = buttonDescriptionI18nKey.IsNullOrEmpty()
            ? ""
            : i18nManager.GetTranslation(buttonDescriptionI18nKey);
        uiButtonDescriptionText.SetText(description);
    }
}
