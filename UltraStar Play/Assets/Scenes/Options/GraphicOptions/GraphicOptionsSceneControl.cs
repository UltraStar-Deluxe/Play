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
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.resolutionContainer)]
    private VisualElement resolutionContainer;

    [Inject(UxmlName = R.UxmlNames.fpsContainer)]
    private VisualElement fpsContainer;

    [Inject(UxmlName = R.UxmlNames.fullscreenContainer)]
    private VisualElement fullscreenContainer;

    [Inject]
    private Settings settings;

    protected override void Start()
    {
        base.Start();
        
        if (PlatformUtils.IsStandalone)
        {
            new ScreenResolutionPickerControl(resolutionContainer.Q<ItemPicker>(), settings);
            new FullscreenModePickerControl(fullscreenContainer.Q<ItemPicker>(), settings, gameObject);
        }
        else
        {
            resolutionContainer.HideByDisplay();
            fullscreenContainer.HideByDisplay();
        }

        List<int> fpsOptions = new() { 30, 60 };
        new LabeledItemPickerControl<int>(fpsContainer.Q<ItemPicker>(), fpsOptions)
            .Bind(() => settings.GraphicSettings.targetFps,
                newValue => settings.GraphicSettings.targetFps = newValue);
    }

    public void UpdateTranslation()
    {
        resolutionContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_resolution);
        fpsContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_targetFps);
        fullscreenContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_fullscreenMode);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        ApplyGraphicSettings();
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
            && res.RefreshRate > 0)
        {
            Screen.SetResolution(res.Width, res.Height, fullScreenMode, res.RefreshRate);
        }
        else
        {
            Debug.LogWarning($"Attempt to apply invalid screen resolution: {res}");
        }
    }
}
