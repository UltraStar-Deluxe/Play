using System.Collections.Generic;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
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

        lastScreenResolution = settings.GraphicSettings.resolution;
        lastFullscreenMode = settings.GraphicSettings.fullScreenMode;
        
        applyResolutionButton.RegisterCallbackButtonTriggered(() => ApplyGraphicSettings());
        
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

        List<int> fpsOptions = new() { 30, 60 };
        new LabeledItemPickerControl<int>(targetFpsPicker, fpsOptions)
            .Bind(() => settings.GraphicSettings.targetFps,
                newValue => settings.GraphicSettings.targetFps = newValue);
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

        ScreenResolution res = settings.GraphicSettings.resolution;
        FullScreenMode fullScreenMode = settings.GraphicSettings.fullScreenMode;
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
