using System.Collections.Generic;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GraphicOptionsSceneControl : MonoBehaviour, INeedInjection, ITranslator
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.resolutionContainer)]
    private VisualElement resolutionContainer;

    [Inject(UxmlName = R.UxmlNames.fpsContainer)]
    private VisualElement fpsContainer;

    [Inject(UxmlName = R.UxmlNames.fullscreenContainer)]
    private VisualElement fullscreenContainer;

    [Inject]
    private Settings settings;

    private void Start()
    {
        if (PlatformUtils.IsStandalone)
        {
            new ScreenResolutionPickerControl(resolutionContainer.Q<ItemPicker>());
            new FullscreenModePickerControl(fullscreenContainer.Q<ItemPicker>(), gameObject);
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

        backButton.RegisterCallbackButtonTriggered(() => ApplyGraphicSettingsAndExitScene());
        backButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => ApplyGraphicSettingsAndExitScene());
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        resolutionContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_resolution);
        fpsContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_targetFps);
        fullscreenContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_fullscreenMode);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options_graphics_title);
    }

    private void ApplyGraphicSettingsAndExitScene()
    {
        ApplyGraphicSettings();
        sceneNavigator.LoadScene(EScene.OptionsScene);
    }

    private void ApplyGraphicSettings()
    {
        if (!PlatformUtils.IsStandalone)
        {
            return;
        }

        ScreenResolution res = SettingsManager.Instance.Settings.GraphicSettings.resolution;
        FullScreenMode fullScreenMode = SettingsManager.Instance.Settings.GraphicSettings.fullScreenMode;
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
