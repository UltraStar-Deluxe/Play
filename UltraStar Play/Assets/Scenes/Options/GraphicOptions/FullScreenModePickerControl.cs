using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class FullScreenModePickerControl : EnumItemPickerControl<EFullScreenMode>
{
    public FullScreenModePickerControl(ItemPicker itemPicker, Settings settings, GameObject gameObject)
        : base(itemPicker, EnumUtils.GetValuesAsList<EFullScreenMode>())
    {
        if (Application.isEditor)
        {
            Selection.Value = EFullScreenMode.Windowed;
        }
        else
        {
            Selection.Value = Screen.fullScreenMode.ToCustomFullScreenMode();
            // The full-screen mode can change, e.g., via global keyboard shortcut. Thus, synchronize with the settings.
            settings.ObserveEveryValueChanged(it => it.FullScreenMode)
                .Subscribe(newFullScreenMode =>
                {
                    // Avoid infinite recursion.
                    if (newFullScreenMode != Selection.Value)
                    {
                        Selection.Value = newFullScreenMode;
                    }
                })
                .AddTo(gameObject);
        }
        Selection.Subscribe(newValue => settings.FullScreenMode = newValue);
    }
}
