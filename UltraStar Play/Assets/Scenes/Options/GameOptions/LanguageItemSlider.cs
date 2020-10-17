using UnityEngine;
using UniRx;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LanguageItemSlider : TextItemSlider<SystemLanguage>, INeedInjection
{
    protected override void Start()
    {
        base.Start();
        Items = EnumUtils.GetValuesAsList<SystemLanguage>();
        SystemLanguage currentLanguage = SettingsManager.Instance.Settings.GameSettings.language;
        Selection.Value = currentLanguage;
        Selection.Subscribe(newValue =>
        {
            SettingsManager.Instance.Settings.GameSettings.language = newValue;
            I18NManager.Instance.UpdateCurrentLanguageAndTranslations(() => I18NManager.UpdateAllTranslationsInScene());
        });
    }

    protected override string GetDisplayString(SystemLanguage item)
    {
        return item.ToString();
    }
}
