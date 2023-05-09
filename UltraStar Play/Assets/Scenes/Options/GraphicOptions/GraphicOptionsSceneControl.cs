using System.Collections.Generic;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GraphicOptionsSceneControl : AbstractOptionsSceneControl, INeedInjection, ITranslator
{
    [Inject(UxmlName = R.UxmlNames.resolutionPicker)]
    private ItemPicker resolutionPicker;

    [Inject(UxmlName = R.UxmlNames.targetFpsPicker)]
    private ItemPicker targetFpsPicker;

    [Inject(UxmlName = R.UxmlNames.fullscreenModePicker)]
    private ItemPicker fullscreenModePicker;

    [Inject(UxmlName = R.UxmlNames.applyResolutionButton)]
    private Button applyResolutionButton;
    
    ScreenResolution lastScreenResolution;
    FullScreenMode lastFullscreenMode;
    
    protected override void Start()
    {
        base.Start();

        lastScreenResolution = settings.ScreenResolution;
        lastFullscreenMode = settings.FullScreenMode;
        
        applyResolutionButton.RegisterCallbackButtonTriggered(_ => ApplyGraphicSettings());
        
        if (PlatformUtils.IsStandalone)
        {
            new ScreenResolutionPickerControl(resolutionPicker, settings);
            new FullscreenModePickerControl(fullscreenModePicker, settings, gameObject);
        }
        else
        {
            resolutionPicker.HideByDisplay();
            fullscreenModePicker.HideByDisplay();
        }

        List<int> fpsOptions = new() { -1, 30, 60 };
        LabeledItemPickerControl<int> targetFpsPickerControl = new(targetFpsPicker, fpsOptions);
        targetFpsPickerControl.GetLabelTextFunction = newValue =>
        {
            if (newValue <= 0)
            {
                return TranslationManager.GetTranslation(R.Messages.options_sampleRate_auto);
            }

            return newValue.ToString();
        };
        targetFpsPickerControl.Bind(() => settings.TargetFps,
                newValue => settings.TargetFps = newValue);
    }

    public void UpdateTranslation()
    {
        resolutionPicker.Label = TranslationManager.GetTranslation(R.Messages.options_resolution);
        targetFpsPicker.Label = TranslationManager.GetTranslation(R.Messages.options_targetFps);
        fullscreenModePicker.Label = TranslationManager.GetTranslation(R.Messages.options_fullscreenMode);
    }

    private void ApplyGraphicSettings()
    {
        if (!PlatformUtils.IsStandalone)
        {
            return;
        }

        ScreenResolution res = settings.ScreenResolution;
        FullScreenMode fullScreenMode = settings.FullScreenMode;
        if (res.Width > 0
            && res.Height > 0
            && res.RefreshRate > 0
            
            && (res.Width != lastScreenResolution.Width
                || res.Height != lastScreenResolution.Height
                || res.RefreshRate != lastScreenResolution.RefreshRate
                || fullScreenMode != lastFullscreenMode) )
        {
            Screen.SetResolution(res.Width, res.Height, fullScreenMode, res.RefreshRate);
            
            // Reload scene.
            // The RenderTextures (UI, scene transition) are recreated when the Screen resolution does not match anymore.
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(2,
                () => sceneNavigator.LoadScene(EScene.OptionsScene, new OptionsSceneData(EScene.OptionsGraphicsScene))));
        }
        else
        {
            Debug.LogWarning($"Attempt to apply invalid screen resolution: {res}");
        }
    }
}
