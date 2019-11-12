using UnityEngine;
using UniRx;

public class LanguageItemSlider : TextItemSlider<SystemLanguage>
{
    protected override void Start()
    {
        base.Start();
        Items = EnumUtils.GetValuesAsList<SystemLanguage>();
        SystemLanguage currentLanguage = SettingsManager.Instance.Settings.GameSettings.language;
        Selection.Value = currentLanguage;
        Selection.Subscribe(newValue => SettingsManager.Instance.Settings.GameSettings.language = newValue);
    }

    protected override string GetDisplayString(SystemLanguage item)
    {
        return item.ToString();
    }
}
