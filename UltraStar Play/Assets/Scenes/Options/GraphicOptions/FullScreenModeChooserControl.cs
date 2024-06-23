using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class FullScreenModeChooserControl : EnumChooserControl<EFullScreenMode>
{
    public FullScreenModeChooserControl(Chooser chooser, Settings settings, GameObject gameObject)
        : base(chooser, EnumUtils.GetValuesAsList<EFullScreenMode>())
    {
        if (Application.isEditor)
        {
            Selection = EFullScreenMode.Windowed;
        }
        else
        {
            Selection = Screen.fullScreenMode.ToCustomFullScreenMode();
            // The full-screen mode can change, e.g., via global keyboard shortcut. Thus, synchronize with the settings.
            settings.ObserveEveryValueChanged(it => it.FullScreenMode)
                .Subscribe(newFullScreenMode =>
                {
                    // Avoid infinite recursion.
                    if (newFullScreenMode != Selection)
                    {
                        Selection = newFullScreenMode;
                    }
                })
                .AddTo(gameObject);
        }
        SelectionAsObservable.Subscribe(newValue => settings.FullScreenMode = newValue);
    }
}
