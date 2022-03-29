using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ThemeSliderControl : LabeledItemPickerControl<Theme>
{
    public ThemeSliderControl(ItemPicker itemPicker)
        : base(itemPicker, new List<Theme>())
    {
        GetLabelTextFunction = item => item != null
            ? item.Name
            : "";

        if (ThemeManager.GetThemes().IsNullOrEmpty())
        {
            ThemeManager.Instance.ReloadThemes();
        }

        Items = ThemeManager.GetThemes();

        if (Items.Count == 0)
        {
            Debug.LogError("No themes have been loaded!");
            return;
        }

        Theme selectedTheme = Items.FirstOrDefault(theme => theme.Name == SettingsManager.Instance.Settings.GraphicSettings.themeName);
        if (selectedTheme == null)
        {
            Selection.Value = Items[0];
        }
        else
        {
            Selection.Value = selectedTheme;
        }

        Selection.Subscribe(newValue =>
        {
            if (newValue == null)
            {
                return;
            }
            SettingsManager.Instance.Settings.GraphicSettings.themeName = newValue.Name;
            ThemeManager.CurrentTheme = newValue;
            ThemeManager.Instance.UpdateThemeResources();
        });
    }
}
