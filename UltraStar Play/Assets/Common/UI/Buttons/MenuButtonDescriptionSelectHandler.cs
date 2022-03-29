using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.EventSystems;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MenuButtonDescriptionSelectHandler : MonoBehaviour, ISelectHandler, INeedInjection
{
    public string buttonDescriptionI18nKey;

    [Inject]
    private MenuButtonDescriptionText uiButtonDescriptionText;

    public void OnSelect(BaseEventData eventData)
    {
        if (uiButtonDescriptionText == null)
        {
            return;
        }

        string description = buttonDescriptionI18nKey.IsNullOrEmpty()
            ? ""
            : TranslationManager.GetTranslation(buttonDescriptionI18nKey);
        uiButtonDescriptionText.SetText(description);
    }
}
