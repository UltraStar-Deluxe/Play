using ProTrans;
using UnityEngine;
using UniRx;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LanguageItemSlider : TextItemSlider<SystemLanguage>, INeedInjection
{
    [Inject]
    private Settings settings;
    
    [Inject]
    private TranslationManager translationManager;
    
    protected override void Start()
    {
        base.Start();
        Items = translationManager.GetTranslatedLanguages();
        SystemLanguage currentLanguage = SettingsManager.Instance.Settings.GameSettings.language;
        Selection.Value = Items.Contains(currentLanguage) ? currentLanguage : SystemLanguage.English;
        Selection.Subscribe(newValue =>
        {
            settings.GameSettings.language = newValue;
            translationManager.currentLanguage = settings.GameSettings.language;
            translationManager.ReloadTranslationsAndUpdateScene();
        });
    }

    protected override string GetDisplayString(SystemLanguage item)
    {
        return item.ToString();
    }
}
