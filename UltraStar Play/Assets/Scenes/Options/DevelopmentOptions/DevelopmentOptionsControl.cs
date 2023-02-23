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

    [Inject(UxmlName = R.UxmlNames.showFpsPicker)]
    private ItemPicker showFpsPicker;

    [Inject(UxmlName = R.UxmlNames.pitchDetectionAlgorithmPicker)]
    private ItemPicker pitchDetectionAlgorithmPicker;

    [Inject(UxmlName = R.UxmlNames.analyzeBeatsWithoutTargetNotePicker)]
    private ItemPicker analyzeBeatsWithoutTargetNotePicker;

    [Inject(UxmlName = R.UxmlNames.disableDynamicThemesPicker)]
    private ItemPicker disableDynamicThemesPicker;

    [Inject(UxmlName = R.UxmlNames.customEventSystemOptInOnAndroidPicker)]
    private ItemPicker customEventSystemOptInOnAndroidPicker;

    [Inject(UxmlName = R.UxmlNames.useUniversalCharsetDetectorPicker)]
    private ItemPicker useUniversalCharsetDetectorPicker;

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

    protected override void Start()
    {
        base.Start();
        
        new BoolPickerControl(showFpsPicker)
            .Bind(() => settings.DeveloperSettings.showFps,
                  newValue => settings.DeveloperSettings.showFps = newValue);

        new PitchDetectionAlgorithmPicker(pitchDetectionAlgorithmPicker)
            .Bind(() => settings.AudioSettings.pitchDetectionAlgorithm,
                newValue => settings.AudioSettings.pitchDetectionAlgorithm = newValue);

        new BoolPickerControl(analyzeBeatsWithoutTargetNotePicker)
            .Bind(() => settings.GraphicSettings.analyzeBeatsWithoutTargetNote,
                newValue => settings.GraphicSettings.analyzeBeatsWithoutTargetNote = newValue);

        new BoolPickerControl(disableDynamicThemesPicker)
            .Bind(() => settings.DeveloperSettings.disableDynamicThemes,
                disableDynamicThemes =>
                {
                    if (disableDynamicThemes)
                    {
                        themeManager.SetCurrentTheme(themeManager.GetDefaultTheme());
                    }
                    settings.DeveloperSettings.disableDynamicThemes = disableDynamicThemes;
                });

        new BoolPickerControl(useUniversalCharsetDetectorPicker)
                    .Bind(() => settings.DeveloperSettings.useUniversalCharsetDetector,
                        newValue => settings.DeveloperSettings.useUniversalCharsetDetector = newValue);

        new BoolPickerControl(customEventSystemOptInOnAndroidPicker)
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
        sceneNavigator.LoadScene(EScene.OptionsScene, new OptionsSceneData(EScene.DevelopmentOptionsScene));
    }

    public void UpdateTranslation()
    {
        showFpsPicker.Label = TranslationManager.GetTranslation(R.Messages.options_showFps);
        pitchDetectionAlgorithmPicker.Label = TranslationManager.GetTranslation(R.Messages.options_pitchDetectionAlgorithm);
        analyzeBeatsWithoutTargetNotePicker.Label = TranslationManager.GetTranslation(R.Messages.options_analyzeBeatsWithoutTargetNote);
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(this);
        return bb.GetBindings();
    }
}
