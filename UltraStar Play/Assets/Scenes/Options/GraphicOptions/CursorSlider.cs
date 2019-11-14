using UnityEngine;
using UniRx;

public class CursorSlider : BoolItemSlider
{

    protected override void Start()
    {
        base.Start();
        Selection.Value = SettingsManager.Instance.Settings.GraphicSettings.useImageAsCursor;
        Selection.Subscribe(newValue => SettingsManager.Instance.Settings.GraphicSettings.useImageAsCursor = newValue);
    }

}
