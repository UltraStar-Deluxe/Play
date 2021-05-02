using UnityEngine;
using UniRx;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LanguageItemSlider : TextItemSlider<SystemLanguage>, INeedInjection
{
    [Inject]
    private I18NManager i18nManager;
    
    protected override void Start()
    {
        base.Start();
        Items = i18nManager.GetTranslatedLanguages();
        SystemLanguage currentLanguage = SettingsManager.Instance.Settings.GameSettings.language;
        Selection.Value = Items.Contains(currentLanguage) ? currentLanguage : SystemLanguage.English;
        Selection.Subscribe(newValue =>
        {
            SettingsManager.Instance.Settings.GameSettings.language = newValue;
            i18nManager.UpdateCurrentLanguageAndTranslations();
        });
    }

    protected override string GetDisplayString(SystemLanguage item)
    {
        return item.ToString();
    }
}
