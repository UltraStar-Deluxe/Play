using UnityEngine;
using UniRx;

public class CursorImageSlider : BoolItemSlider
{

    void Start()
    {
        base.Start();
        Selection.Value = SettingsManager.Instance.Settings.GraphicSettings.useImageAsCursor;
        Selection.Subscribe(newValue => SettingsManager.Instance.Settings.GraphicSettings.useImageAsCursor = newValue);
    }

}
