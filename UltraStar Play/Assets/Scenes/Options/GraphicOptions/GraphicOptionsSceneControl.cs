using System.Collections.Generic;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GraphicOptionsSceneControl : AbstractOptionsSceneControl, INeedInjection
{
    [Inject(UxmlName = R.UxmlNames.resolutionChooser)]
    private Chooser resolutionChooser;

    [Inject(UxmlName = R.UxmlNames.targetFpsChooser)]
    private Chooser targetFpsChooser;

    [Inject(UxmlName = R.UxmlNames.fullscreenModeChooser)]
    private Chooser fullscreenModeChooser;

    [Inject(UxmlName = R.UxmlNames.applyResolutionButton)]
    private Button applyResolutionButton;

    ScreenResolution lastScreenResolution;
    EFullScreenMode lastFullscreenMode;

    protected override void Start()
    {
        base.Start();

        lastScreenResolution = settings.ScreenResolution;
        lastFullscreenMode = settings.FullScreenMode;

        applyResolutionButton.RegisterCallbackButtonTriggered(_ => ApplyGraphicSettings());

        if (PlatformUtils.IsStandalone)
        {
            new ScreenResolutionChooserControl(resolutionChooser, settings);
            new FullScreenModeChooserControl(fullscreenModeChooser, settings, gameObject);
        }
        else
        {
            resolutionChooser.HideByDisplay();
            fullscreenModeChooser.HideByDisplay();
        }

        TargetFpsChooserControl targetFpsChooserControl = new(targetFpsChooser);
        targetFpsChooserControl.Bind(() => settings.TargetFps,
                newValue => settings.TargetFps = newValue);
    }

    private void ApplyGraphicSettings()
    {
        if (!PlatformUtils.IsStandalone)
        {
            return;
        }

        ScreenResolution res = settings.ScreenResolution;
        EFullScreenMode fullScreenMode = settings.FullScreenMode;
        if (res.Width > 0
            && res.Height > 0
            && res.RefreshRate > 0

            && (res.Width != lastScreenResolution.Width
                || res.Height != lastScreenResolution.Height
                || res.RefreshRate != lastScreenResolution.RefreshRate
                || fullScreenMode != lastFullscreenMode) )
        {
            Screen.SetResolution(res.Width, res.Height, fullScreenMode.ToUnityFullScreenMode(), res.RefreshRate);

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
