using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class FullscreenModePickerControl : LabeledItemPickerControl<FullScreenMode>
{
    public FullscreenModePickerControl(ItemPicker itemPicker, Settings settings, GameObject gameObject)
        : base(itemPicker, EnumUtils.GetValuesAsList<FullScreenMode>())
    {
        if (Application.isEditor)
        {
            Selection.Value = FullScreenMode.Windowed;
        }
        else
        {
            Selection.Value = Screen.fullScreenMode;
            // The full-screen mode can change, e.g., via global keyboard shortcut. Thus, synchronize with the settings.
            settings.GraphicSettings
                .ObserveEveryValueChanged(it => it.fullScreenMode)
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
        Selection.Subscribe(newValue => settings.GraphicSettings.fullScreenMode = newValue);
    }
}
