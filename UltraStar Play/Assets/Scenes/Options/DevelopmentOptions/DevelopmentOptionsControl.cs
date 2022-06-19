using System.Collections.Generic;
using System.IO;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using Serilog.Events;
using Serilog.Formatting.Display;
using SimpleHttpServerForUnity;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class DevelopmentOptionsControl : MonoBehaviour, INeedInjection, ITranslator
{
    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private TranslationManager translationManager;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.showFpsContainer)]
    private VisualElement showFpsContainer;

    [Inject(UxmlName = R.UxmlNames.pitchDetectionAlgorithmContainer)]
    private VisualElement pitchDetectionAlgorithmContainer;

    [Inject(UxmlName = R.UxmlNames.analyzeBeatsWithoutTargetNoteContainer)]
    private VisualElement analyzeBeatsWithoutTargetNoteContainer;

    [Inject(UxmlName = R.UxmlNames.ipAddressLabel)]
    private Label ipAddressLabel;

    [Inject(UxmlName = R.UxmlNames.httpServerPortLabel)]
    private Label httpServerPortLabel;

    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;

    [Inject(UxmlName = R.UxmlNames.logLevelItemPicker)]
    private ItemPicker logLevelItemPicker;

    [Inject(UxmlName = R.UxmlNames.logTextField)]
    private TextField logTextField;

    [Inject(UxmlName = R.UxmlNames.showLogOverlayButton)]
    private Button showLogOverlayButton;

    [Inject(UxmlName = R.UxmlNames.closeLogOverlayButton)]
    private Button closeLogOverlayButton;

    [Inject(UxmlName = R.UxmlNames.logOverlay)]
    private VisualElement logOverlay;

    [Inject]
    private Settings settings;

    [Inject]
    private UiManager uiManager;

    private LabeledItemPickerControl<LogEventLevel> logLevelItemPickerControl;

    private void Start()
    {
        new BoolPickerControl(showFpsContainer.Q<ItemPicker>())
            .Bind(() => settings.DeveloperSettings.showFps,
                  newValue =>
                  {
                      settings.DeveloperSettings.showFps = newValue;
                      if (newValue)
                      {
                        uiManager.CreateShowFpsInstance();
                      }
                      else
                      {
                        uiManager.DestroyShowFpsInstance();
                      }
                  });

        new PitchDetectionAlgorithmPicker(pitchDetectionAlgorithmContainer.Q<ItemPicker>())
            .Bind(() => settings.AudioSettings.pitchDetectionAlgorithm,
                newValue => settings.AudioSettings.pitchDetectionAlgorithm = newValue);

        new BoolPickerControl(analyzeBeatsWithoutTargetNoteContainer.Q<ItemPicker>())
            .Bind(() => settings.GraphicSettings.analyzeBeatsWithoutTargetNote,
                newValue => settings.GraphicSettings.analyzeBeatsWithoutTargetNote = newValue);

        ipAddressLabel.text = TranslationManager.GetTranslation(R.Messages.options_ipAddress,
            "value", HttpServer.Instance.host);

        if (HttpServer.IsSupported)
        {
            httpServerPortLabel.text = TranslationManager.GetTranslation(R.Messages.options_httpServerPortWithExampleUri,
                "host", HttpServer.Instance.host,
                "port", HttpServer.Instance.port);
        }
        else
        {
            httpServerPortLabel.text = TranslationManager.GetTranslation(R.Messages.options_httpServerNotSupported);
        }

        InputManager.GetInputAction(R.InputActions.usplay_back).PerformedAsObservable(5)
            .Subscribe(_ => OnBack());

        // View Log
        HideLogOverlay();
        showLogOverlayButton.RegisterCallbackButtonTriggered(() => ShowLogOverlay());
        closeLogOverlayButton.RegisterCallbackButtonTriggered(() => HideLogOverlay());
        logLevelItemPickerControl = new LabeledItemPickerControl<LogEventLevel>(
            logLevelItemPicker,
            EnumUtils.GetValuesAsList<LogEventLevel>());
        LogEvent eventWithHighestLogLevel = Log.GetLogHistory()
            .FindMaxElement(logEvent =>  (int)logEvent.Level);
        LogEventLevel highestLogLevel = eventWithHighestLogLevel != null
            ? eventWithHighestLogLevel.Level
            : LogEventLevel.Error;
        logLevelItemPickerControl.SelectItem(highestLogLevel);
        logLevelItemPickerControl.Selection.Subscribe(_ => UpdateLogTextField());
        UpdateLogTextField();

        // Back button
        backButton.RegisterCallbackButtonTriggered(() => OnBack());
        backButton.Focus();
    }

    private void HideLogOverlay()
    {
        logOverlay.HideByDisplay();
        showLogOverlayButton.Focus();
    }

    private void ShowLogOverlay()
    {
        logOverlay.ShowByDisplay();
        closeLogOverlayButton.Focus();
    }

    private void OnBack()
    {
        if (logOverlay.IsVisibleByDisplay())
        {
            HideLogOverlay();
        }
        else
        {
            sceneNavigator.LoadScene(EScene.OptionsScene);
        }
    }

    private void UpdateLogTextField()
    {
        MessageTemplateTextFormatter textFormatter = new(Log.OutputTemplate);
        List<string> logLines = Log.GetLogHistory()
            .Where(logEvent => (int)logEvent.Level >= (int)logLevelItemPickerControl.SelectedItem)
            .Select(logEvent =>
            {
                StringWriter stringWriter = new();
                textFormatter.Format(logEvent, stringWriter);
                string logLine = stringWriter.ToString();
                // Workaround for Unity TextField interpreting backslash for special characters.
                return logLine.Replace("\\", "/");
            })
            .ToList();
        logTextField.value = logLines.IsNullOrEmpty()
            ? "(no messages for this log level)"
            : logLines.JoinWith("");
    }

    public void UpdateTranslation()
    {
        if (!Application.isPlaying && backButton == null)
        {
            SceneInjectionManager.Instance.DoInjection();
        }
        showFpsContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_showFps);
        pitchDetectionAlgorithmContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_pitchDetectionAlgorithm);
        analyzeBeatsWithoutTargetNoteContainer.Q<Label>().text = TranslationManager.GetTranslation(R.Messages.options_analyzeBeatsWithoutTargetNote);
        backButton.text = TranslationManager.GetTranslation(R.Messages.back);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.options_development_title);
    }
}
