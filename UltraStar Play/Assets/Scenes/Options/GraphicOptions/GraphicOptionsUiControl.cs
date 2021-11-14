using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class GraphicOptionsUiControl : MonoBehaviour, INeedInjection, ITranslator
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
        new ScreenResolutionPickerControl(resolutionContainer.Q<ItemPicker>());

        List<int> fpsOptions = new List<int> { 30, 60 };
        new LabeledItemPickerControl<int>(fpsContainer.Q<ItemPicker>(), fpsOptions)
            .Bind(() => settings.GraphicSettings.targetFps,
                newValue => settings.GraphicSettings.targetFps = newValue);

        new FullscreenModePickerControl(fullscreenContainer.Q<ItemPicker>());

        backButton.RegisterCallbackButtonTriggered(() => ApplyGraphicSettingsAndExitScene());

        resolutionContainer.Q<ItemPicker>().PreviousItemButton.Focus();

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => ApplyGraphicSettingsAndExitScene());
    }

    private void ApplyGraphicSettingsAndExitScene()
    {
        ApplyGraphicSettings();
        sceneNavigator.LoadScene(EScene.OptionsScene);
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && resolutionContainer == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        resolutionContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_resolution);
        fpsContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_targetFps);
        fullscreenContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_fullscreenMode);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.graphicOptionsScene_title);
    }

    void ApplyGraphicSettings()
    {
        ScreenResolution res = SettingsManager.Instance.Settings.GraphicSettings.resolution;
        FullScreenMode fullScreenMode = SettingsManager.Instance.Settings.GraphicSettings.fullScreenMode;
        Screen.SetResolution(res.Width, res.Height, fullScreenMode, res.RefreshRate);
    }
}