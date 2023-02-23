using System.Collections.Generic;
using PrimeInputActions;
using ProTrans;
using Serilog.Events;
using SimpleHttpServerForUnity;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DevelopmentOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator, IBinder
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.showFpsContainer)]
    private VisualElement showFpsContainer;

    [Inject(UxmlName = R.UxmlNames.pitchDetectionAlgorithmContainer)]
    private VisualElement pitchDetectionAlgorithmContainer;

    [Inject(UxmlName = R.UxmlNames.analyzeBeatsWithoutTargetNoteContainer)]
    private VisualElement analyzeBeatsWithoutTargetNoteContainer;

    [Inject(UxmlName = R.UxmlNames.disableDynamicThemesContainer)]
    private VisualElement disableDynamicThemesContainer;

    [Inject(UxmlName = R.UxmlNames.customEventSystemOptInOnAndroidContainer)]
    private VisualElement customEventSystemOptInOnAndroidContainer;

    [Inject(UxmlName = R.UxmlNames.useUniversalCharsetDetectorContainer)]
    private VisualElement useUniversalCharsetDetectorContainer;

    [Inject(UxmlName = R.UxmlNames.ipAddressLabel)]
    private Label ipAddressLabel;

    [Inject(UxmlName = R.UxmlNames.httpServerPortLabel)]
    private Label httpServerPortLabel;

    [Inject(UxmlName = R.UxmlNames.showLogButton)]
    private Button showLogButton;

    [Inject(UxmlName = R.UxmlNames.copyLogButton)]
    private Button copyLogButton;

    [Inject(UxmlName = R.UxmlNames.openPersistentDataPathButton)]
    private Button openPersistentDataPathButton;

    [Inject]
    private Settings settings;

    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    [Inject]
    private HttpServer httpServer;

    [Inject]
    private InGameDebugConsoleManager inGameDebugConsoleManager;

    private NetworkConfigControl networkConfigControl;

    private void Start()
    {
        new BoolPickerControl(showFpsContainer.Q<ItemPicker>())
            .Bind(() => settings.DeveloperSettings.showFps,
                  newValue => settings.DeveloperSettings.showFps = newValue);

        new PitchDetectionAlgorithmPicker(pitchDetectionAlgorithmContainer.Q<ItemPicker>())
            .Bind(() => settings.AudioSettings.pitchDetectionAlgorithm,
                newValue => settings.AudioSettings.pitchDetectionAlgorithm = newValue);

        new BoolPickerControl(analyzeBeatsWithoutTargetNoteContainer.Q<ItemPicker>())
            .Bind(() => settings.GraphicSettings.analyzeBeatsWithoutTargetNote,
                newValue => settings.GraphicSettings.analyzeBeatsWithoutTargetNote = newValue);

        new BoolPickerControl(disableDynamicThemesContainer.Q<ItemPicker>())
            .Bind(() => settings.DeveloperSettings.disableDynamicThemes,
                disableDynamicThemes =>
                {
                    if (disableDynamicThemes)
                    {
                        themeManager.SetCurrentTheme(themeManager.GetDefaultTheme());
                    }
                    settings.DeveloperSettings.disableDynamicThemes = disableDynamicThemes;
                });

        new BoolPickerControl(useUniversalCharsetDetectorContainer.Q<ItemPicker>())
                    .Bind(() => settings.DeveloperSettings.useUniversalCharsetDetector,
                        newValue => settings.DeveloperSettings.useUniversalCharsetDetector = newValue);

        new BoolPickerControl(customEventSystemOptInOnAndroidContainer.Q<ItemPicker>())
            .Bind(() => settings.DeveloperSettings.enableEventSystemOnAndroid,
                newValue =>
                {
                    if (newValue != settings.DeveloperSettings.enableEventSystemOnAndroid)
                    {
                        settings.DeveloperSettings.enableEventSystemOnAndroid = newValue;
                        RestartScene();
                    }
                });

        ipAddressLabel.text = TranslationManager.GetTranslation(R.Messages.options_ipAddress,
            "value", httpServer.host);

        if (HttpServer.IsSupported)
        {
            httpServerPortLabel.text = TranslationManager.GetTranslation(R.Messages.options_httpServerPortWithExampleUri,
                "host", httpServer.host,
                "port", httpServer.port);
        }
        else
        {
            httpServerPortLabel.text = TranslationManager.GetTranslation(R.Messages.options_httpServerNotSupported);
        }

        // View and copy log
        showLogButton.RegisterCallbackButtonTriggered(() => inGameDebugConsoleManager.ShowConsole());
        copyLogButton.RegisterCallbackButtonTriggered(() =>
        {
            ClipboardUtils.CopyToClipboard(Log.GetLogText(LogEventLevel.Verbose));
            UiManager.CreateNotification("Copied log to clipboard");
        });

        // Open persistent data path
        if (PlatformUtils.IsStandalone)
        {
            openPersistentDataPathButton.RegisterCallbackButtonTriggered(() => ApplicationUtils.OpenDirectory(Application.persistentDataPath));
        }
        else
        {
            openPersistentDataPathButton.HideByDisplay();
        }
        
        // Network config
        networkConfigControl = injector.CreateAndInject<NetworkConfigControl>();
    }

    private void RestartScene()
    {
        sceneNavigator.LoadScene(EScene.DevelopmentOptionsScene);
    }

    public void UpdateTranslation()
    {
        showFpsContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_showFps);
        pitchDetectionAlgorithmContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_pitchDetectionAlgorithm);
        analyzeBeatsWithoutTargetNoteContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_analyzeBeatsWithoutTargetNote);
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(this);
        return bb.GetBindings();
    }
}
