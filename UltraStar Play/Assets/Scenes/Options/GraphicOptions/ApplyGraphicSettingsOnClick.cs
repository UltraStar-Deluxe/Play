using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

[RequireComponent(typeof(Button))]
public class ApplyGraphicSettingsOnClick : MonoBehaviour
{
    void Start()
    {
        Button button = GetComponent<Button>();
        button.OnClickAsObservable().Subscribe(_ => ApplyGraphicSettings());
    }

    void ApplyGraphicSettings()
    {
        ScreenResolution res = SettingsManager.Instance.Settings.GraphicSettings.resolution;
        FullScreenMode fullScreenMode = SettingsManager.Instance.Settings.GraphicSettings.fullScreenMode;
        Screen.SetResolution(res.Width, res.Height, fullScreenMode, res.RefreshRate);
    }
}
