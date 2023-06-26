using System;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using Serilog.Events;
using SimpleHttpServerForUnity;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DevelopmentOptionsControl : AbstractOptionsSceneControl, INeedInjection, ITranslator, IBinder
{
    [Inject(UxmlName = R.UxmlNames.showFpsPicker)]
    private ItemPicker showFpsPicker;

    [Inject(UxmlName = R.UxmlNames.streamAudioInSingScenePicker)]
    private ItemPicker streamAudioInSingScenePicker;

    [Inject(UxmlName = R.UxmlNames.pitchDetectionAlgorithmPicker)]
    private ItemPicker pitchDetectionAlgorithmPicker;

    [Inject(UxmlName = R.UxmlNames.analyzeBeatsWithoutTargetNotePicker)]
    private ItemPicker analyzeBeatsWithoutTargetNotePicker;

    [Inject(UxmlName = R.UxmlNames.animatedBackgroundItemPicker)]
    private ItemPicker animatedBackgroundItemPicker;

    [Inject(UxmlName = R.UxmlNames.disableDynamicThemesPicker)]
    private ItemPicker disableDynamicThemesPicker;

    [Inject(UxmlName = R.UxmlNames.customEventSystemOptInOnAndroidPicker)]
    private ItemPicker customEventSystemOptInOnAndroidPicker;

    [Inject(UxmlName = R.UxmlNames.useUniversalCharsetDetectorPicker)]
    private ItemPicker useUniversalCharsetDetectorPicker;

    [Inject(UxmlName = R.UxmlNames.connectionEndpointLabel)]
    private Label connectionEndpointLabel;

    [Inject(UxmlName = R.UxmlNames.httpEndpointExampleLabel)]
    private Label httpEndpointExampleLabel;

    [Inject(UxmlName = R.UxmlNames.showLogButton)]
    private Button showLogButton;

    [Inject(UxmlName = R.UxmlNames.copyLogButton)]
    private Button copyLogButton;

    [Inject(UxmlName = R.UxmlNames.openPersistentDataPathButton)]
    private Button openPersistentDataPathButton;
    
    [Inject(UxmlName = R.UxmlNames.openWebViewScriptsPathButton)]
    private Button openWebViewScriptsPathButton;
    
    [Inject(UxmlName = R.UxmlNames.messageBufferTimeTextField)]
    private IntegerField messageBufferTimeTextField;

    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    [Inject]
    private UltraStarPlayHttpServer httpServer;
    
    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private InGameDebugConsoleManager inGameDebugConsoleManager;
    
    [Inject(UxmlName = R.UxmlNames.audioSeparationCommandTextField)]
    private TextField audioSeparationCommandTextField;
    
    [Inject(UxmlName = R.UxmlNames.basicPitchCommandTextField)]
    private TextField basicPitchCommandTextField;
    
    [Inject(UxmlName = R.UxmlNames.clientDiscoveryPortTextField)]
    private IntegerField clientDiscoveryPortTextField;
    
    [Inject(UxmlName = R.UxmlNames.httpServerHostTextField)]
    private TextField httpServerHostTextField;
    
    [Inject(UxmlName = R.UxmlNames.httpServerPortTextField)]
    private IntegerField httpServerPortTextField;

    [Inject(UxmlName = R.UxmlNames.minimumLogLevelPicker)]
    private ItemPicker minimumLogLevelPicker;
    
    protected override void Start()
    {
        base.Start();
        
        new BoolPickerControl(showFpsPicker)
            .Bind(() => settings.ShowFps,
                  newValue => settings.ShowFps = newValue);

        List<LogEventLevel> logEventLevels = EnumUtils.GetValuesAsList<LogEventLevel>()
            .OrderBy(logEventLevel => (int)logEventLevel)
            .ToList();
        new LabeledItemPickerControl<LogEventLevel>(minimumLogLevelPicker, logEventLevels)
            .Bind(() => settings.MinimumLogLevel,
                  newValue =>
                  {
                      settings.MinimumLogLevel = newValue;
                      UpdateLogEventLevel();
                  });
        
        new BoolPickerControl(streamAudioInSingScenePicker)
            .Bind(() => settings.StreamAudioInSingScene,
                newValue => settings.StreamAudioInSingScene = newValue);

        new PitchDetectionAlgorithmPickerControl(pitchDetectionAlgorithmPicker)
            .Bind(() => settings.PitchDetectionAlgorithm,
                newValue => settings.PitchDetectionAlgorithm = newValue);

        new BoolPickerControl(analyzeBeatsWithoutTargetNotePicker)
            .Bind(() => settings.AnalyzeBeatsWithoutTargetNote,
                newValue => settings.AnalyzeBeatsWithoutTargetNote = newValue);

        new BoolPickerControl(animatedBackgroundItemPicker)
            .Bind(() => settings.AnimatedBackground,
                newValue => settings.AnimatedBackground = newValue);

        new BoolPickerControl(disableDynamicThemesPicker)
            .Bind(() => settings.DisableDynamicThemes,
                disableDynamicThemes =>
                {
                    if (disableDynamicThemes)
                    {
                        themeManager.SetCurrentTheme(themeManager.GetDefaultTheme());
                    }
                    settings.DisableDynamicThemes = disableDynamicThemes;
                });

        new BoolPickerControl(useUniversalCharsetDetectorPicker)
                    .Bind(() => settings.UseUniversalCharsetDetector,
                        newValue => settings.UseUniversalCharsetDetector = newValue);

        customEventSystemOptInOnAndroidPicker.SetVisibleByDisplay(PlatformUtils.IsAndroid);
        new BoolPickerControl(customEventSystemOptInOnAndroidPicker)
            .Bind(() => settings.EnableEventSystemOnAndroid,
                newValue =>
                {
                    if (newValue != settings.EnableEventSystemOnAndroid)
                    {
                        settings.EnableEventSystemOnAndroid = newValue;
                        RestartScene();
                    }
                });

        connectionEndpointLabel.text = $"Connection endpoint: {serverSideConnectRequestManager.GetConnectionEndpoint()}";

        if (HttpServer.IsSupported)
        {
            httpEndpointExampleLabel.text = "HTTP endpoint example: " + httpServer.GetExampleEndpoint();
        }
        else
        {
            httpEndpointExampleLabel.text = TranslationManager.GetTranslation(R.Messages.options_httpServerNotSupported);
        }

        // View and copy log
        showLogButton.RegisterCallbackButtonTriggered(_ => inGameDebugConsoleManager.ShowConsole());
        copyLogButton.RegisterCallbackButtonTriggered(_ =>
        {
            ClipboardUtils.CopyToClipboard(Log.GetLogHistoryAsText(LogEventLevel.Verbose));
            UiManager.CreateNotification("Copied log to clipboard");
        });

        // Open persistent data path
        if (PlatformUtils.IsStandalone)
        {
            openPersistentDataPathButton.RegisterCallbackButtonTriggered(_ => ApplicationUtils.OpenDirectory(Application.persistentDataPath));
        }
        else
        {
            openPersistentDataPathButton.HideByDisplay();
        }
        
        // Open WebView scripts path
        if (PlatformUtils.IsStandalone)
        {
            openWebViewScriptsPathButton.RegisterCallbackButtonTriggered(_ => ApplicationUtils.OpenDirectory(ApplicationUtils.GetWebViewScriptsAbsolutePath()));
        }
        else
        {
            openWebViewScriptsPathButton.HideByDisplay();
        }
        
        // Message delay
        FieldBindingUtils.Bind(messageBufferTimeTextField,
            () => settings.ConnectedClientMessageBufferTimeInMillis,
            newValue => settings.ConnectedClientMessageBufferTimeInMillis = newValue);
        messageBufferTimeTextField.DisableChangeValueByDragging();
        
        // Spleeter command (audio separation)
        audioSeparationCommandTextField.DisableParseEscapeSequences();
        FieldBindingUtils.Bind(audioSeparationCommandTextField,
            () => settings.SongEditorSettings.AudioSeparationCommand,
            newValue => settings.SongEditorSettings.AudioSeparationCommand = newValue);
        
        // Basic Pitch command (pitch detection)
        basicPitchCommandTextField.DisableParseEscapeSequences();
        FieldBindingUtils.Bind(basicPitchCommandTextField,
            () => settings.SongEditorSettings.BasicPitchCommand,
            newValue => settings.SongEditorSettings.BasicPitchCommand = newValue);
        
        // Network config
        FieldBindingUtils.Bind(clientDiscoveryPortTextField,
            () => settings.ConnectionServerPort,
            newValue => settings.ConnectionServerPort = newValue);
        clientDiscoveryPortTextField.DisableChangeValueByDragging();
        
        FieldBindingUtils.Bind(httpServerHostTextField,
            () => settings.HttpServerHost,
            newValue => settings.HttpServerHost = newValue);
        
        FieldBindingUtils.Bind(httpServerPortTextField,
            () => settings.HttpServerPort,
            newValue => settings.HttpServerPort = newValue);
        httpServerPortTextField.DisableChangeValueByDragging();
    }

    private void UpdateLogEventLevel()
    {
        if (Log.MinimumLogLevel == settings.MinimumLogLevel)
        {
            return;
        }
        
        Log.MinimumLogLevel = settings.MinimumLogLevel;
                      
        Debug.Log("Changed minimum log level to " + settings.MinimumLogLevel + ". The following is for testing log levels...");
        
        Log.Verbose(() => "Serilog verbose log message");
        
        Log.Debug(() => "Serilog debug log message");
        
        Log.Information(() => "Serilog info log message");
        Debug.Log("Unity info log message");
        
        Log.Warning(() => "Serilog warning log message");
        Debug.LogWarning("Unity warning log message");
        
        Log.Error(() => "Serilog error log message");
        Debug.LogError("Unity error log message");
        
        Log.Exception(() => new Exception("Serilog exception log message"));
        Debug.LogException(new Exception("Unity exception message"));
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
