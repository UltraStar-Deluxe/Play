using System;
using System.Collections.Generic;
using System.Linq;
using PrimeInputActions;
using ProTrans;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class RecordingOptionsSceneControl : AbstractOptionsSceneControl, ITranslator, IBinder
{
    private static readonly List<int> amplificationItems = new() { 0, 3, 6, 9, 12, 15, 18 };
    private static readonly List<int> noiseSuppressionItems= new() { 0, 5, 10, 15, 20, 25, 30 };

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private RecordingOptionsMicVisualizer micVisualizer;

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private CalibrateMicDelayControl calibrateMicDelayControl;

    [Inject(SearchMethod = SearchMethods.FindObjectOfType)]
    private MicPitchTracker micPitchTracker;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    [Inject]
    private ThemeManager themeManager;

    [Inject(UxmlName = R.UxmlNames.devicePicker)]
    private ItemPicker devicePicker;

    [Inject(UxmlName = R.UxmlNames.amplificationPicker)]
    private ItemPicker amplificationPicker;

    [Inject(UxmlName = R.UxmlNames.noiseSuppressionPicker)]
    private ItemPicker noiseSuppressionPicker;

    [Inject(UxmlName = R.UxmlNames.delayPicker)]
    private ItemPicker delayPicker;

    [Inject(UxmlName = R.UxmlNames.colorPicker)]
    private ItemPicker colorPicker;

    [Inject(UxmlName = R.UxmlNames.sampleRatePicker)]
    private ItemPicker sampleRatePicker;

    [Inject(UxmlName = R.UxmlNames.enabledToggle)]
    private SlideToggle enabledToggle;

    [Inject(UxmlName = R.UxmlNames.notConnectedContainer)]
    private VisualElement notConnectedContainer;

    [Inject(UxmlName = R.UxmlNames.notConnectedLabel)]
    private Label notConnectedLabel;

    [Inject(UxmlName = R.UxmlNames.deleteButton)]
    private Button deleteButton;

    [Inject(UxmlName = R.UxmlNames.audioWaveForm)]
    private VisualElement audioWaveForm;

    [Inject(UxmlName = R.UxmlNames.noteLabel)]
    private Label noteLabel;

    [Inject(UxmlName = R.UxmlNames.calibrateDelayButton)]
    private Button calibrateDelayButton;

    [Inject(UxmlName = R.UxmlNames.recordingDeviceInactiveOverlay)]
    private VisualElement recordingDeviceInactiveOverlay;
    
    private SampleRatePickerControl sampleRatePickerControl;
    private LabeledItemPickerControl<MicProfile> devicePickerControl;
    private LabeledItemPickerControl<int> amplificationPickerControl;
    private LabeledItemPickerControl<int> noiseSuppressionPickerControl;
    private NumberPickerControl delayPickerControl;
    private ColorPickerControl colorPickerControl;

    private MicProfile SelectedMicProfile => devicePickerControl.SelectedItem;

    private IDisposable connectedClientReceivedMessageStreamDisposable;

    private readonly Subject<BeatPitchEvent> connectedClientBeatPitchEventStream = new();
    public IObservable<BeatPitchEvent> ConnectedClientBeatPitchEventStream => connectedClientBeatPitchEventStream;

    protected override void Start()
    {
        base.Start();
        
        new AutoFitLabelControl(devicePicker.ItemLabel, 10, 15);
        
        devicePickerControl = new LabeledItemPickerControl<MicProfile>(devicePicker, CreateMicProfiles());
        devicePickerControl.AutoSmallFont = false;
        devicePickerControl.GetLabelTextFunction = item => item != null ? item.Name : "";
        if (!TryReSelectLastMicProfile())
        {
            devicePickerControl.Selection.Value = devicePickerControl.Items[0];
        }
        devicePickerControl.Selection
            .Subscribe(micProfile => settings.LastMicProfileNameInRecordingOptionsScene = micProfile?.Name);

        amplificationPickerControl = new LabeledItemPickerControl<int>(amplificationPicker, amplificationItems);
        amplificationPickerControl.GetLabelTextFunction = item => item + " %";
        noiseSuppressionPickerControl = new LabeledItemPickerControl<int>(noiseSuppressionPicker, noiseSuppressionItems);
        noiseSuppressionPickerControl.GetLabelTextFunction = item => item + " %";
        delayPickerControl = new NumberPickerControl(delayPicker);
        delayPickerControl.GetLabelTextFunction = item => item + " ms";
        colorPickerControl = new ColorPickerControl(colorPicker, themeManager.GetMicrophoneColors());
        sampleRatePickerControl = new SampleRatePickerControl(sampleRatePicker);
        sampleRatePickerControl.GetLabelTextFunction = _ => GetSampleRateLabel();
        enabledToggle.RegisterValueChangedCallback(evt => SetSelectedRecordingDeviceEnabled(evt.newValue));
        deleteButton.RegisterCallbackButtonTriggered(_ => DeleteSelectedRecordingDevice());
        
        devicePickerControl.Selection.Subscribe(newValue => OnRecordingDeviceSelected(newValue));
        amplificationPickerControl.Selection.Subscribe(newValue =>
        {
            SelectedMicProfile.Amplification = newValue;
            SendSelectedMicProfileToConnectedClient();
        });
        noiseSuppressionPickerControl.Selection.Subscribe(newValue =>
        {
            SelectedMicProfile.NoiseSuppression = newValue;
            SendSelectedMicProfileToConnectedClient();
        });
        delayPickerControl.Selection.Subscribe(newValue =>
        {
            SelectedMicProfile.DelayInMillis = (int)newValue;
            SendSelectedMicProfileToConnectedClient();
        });
        colorPickerControl.Selection.Subscribe(newValue =>
        {
            SelectedMicProfile.Color = newValue;
            SendSelectedMicProfileToConnectedClient();
        });
        sampleRatePickerControl.Selection.Subscribe(newValue =>
        {
            SelectedMicProfile.SampleRate = newValue;
            SendSelectedMicProfileToConnectedClient();
        });
        micPitchTracker.MicSampleRecorder.FinalSampleRate
            .Subscribe(_ => UpdateSampleRateLabel())
            .AddTo(gameObject);
        micPitchTracker.MicSampleRecorder.IsRecording
            .Subscribe(_ => UpdateSampleRateLabel())
            .AddTo(gameObject);

        // Reselect recording device of connected client, when the client has now connected
        serverSideConnectRequestManager.ClientConnectedEventStream
            .Where(clientConnectedEvent => devicePickerControl.SelectedItem?.ConnectedClientId == clientConnectedEvent.ConnectedClientHandler.ClientId)
            .Subscribe(newValue => OnRecordingDeviceSelected(devicePickerControl.SelectedItem))
            .AddTo(gameObject);

        serverSideConnectRequestManager.ClientConnectedEventStream
            .Subscribe(UpdateMicProfileNames)
            .AddTo(gameObject);

        calibrateDelayButton.RegisterCallbackButtonTriggered(_ => calibrateMicDelayControl.StartCalibration());
        calibrateMicDelayControl.CalibrationResultEventStream
            .Subscribe(calibrationResult =>
            {
                if (calibrationResult.IsSuccess)
                {
                    double medianValue = calibrationResult.DelaysInMilliseconds[calibrationResult.DelaysInMilliseconds.Count / 2];
                    double roundedMedianValue = ((int)(medianValue / delayPickerControl.StepValue)) * delayPickerControl.StepValue;
                    delayPickerControl.SelectItem(roundedMedianValue);
                }
                else
                {
                    UiManager.CreateNotification(
                        TranslationManager.GetTranslation(R.Messages.options_delay_calibrate_timeout));
                }
            });
    }

    private void Update()
    {
        // Read messages from client since last time the reader thread was active.
        IConnectedClientHandler connectedClientHandler = GetConnectedClientHandler();
        connectedClientHandler?.ReadMessagesFromClient();
    }

    private void UpdateSampleRateLabel()
    {
        sampleRatePickerControl.UpdateLabelText();
    }

    private string GetSampleRateLabel()
    {
        int item = sampleRatePickerControl.SelectedItem;
        if (item <= 0)
        {
            // When "auto" is selected, then also show the automatically used sample rate.
            string sampleRateText = SelectedMicProfile.IsInputFromConnectedClient
                ? ""
                : $"\n({micPitchTracker.MicSampleRecorder.FinalSampleRate.Value} Hz)";
            return TranslationManager.GetTranslation(R.Messages.options_sampleRate_auto) + sampleRateText;
        }
        return $"{item} Hz";
    }

    private bool TryReSelectLastMicProfile()
    {
        if (settings.LastMicProfileNameInRecordingOptionsScene.IsNullOrEmpty())
        {
            return false;
        }

        MicProfile lastMicProfile = devicePickerControl.Items
            .FirstOrDefault(micProfile => micProfile.Name == settings.LastMicProfileNameInRecordingOptionsScene);
        if (lastMicProfile == null)
        {
            return false;
        }

        devicePickerControl.SelectItem(lastMicProfile);
        return true;
    }

    private void SetSelectedRecordingDeviceEnabled(bool isEnabled)
    {
        if (SelectedMicProfile == null)
        {
            return;
        }
        SelectedMicProfile.IsEnabled = isEnabled;
        if (isEnabled)
        {
            settings.MicProfiles.AddIfNotContains(SelectedMicProfile);
        }
        
        UpdateRecordingDeviceInactiveOverlay();
    }

    private IConnectedClientHandler GetConnectedClientHandler()
    {
        if (SelectedMicProfile == null
            || SelectedMicProfile.ConnectedClientId.IsNullOrEmpty()
            || !serverSideConnectRequestManager.TryGetConnectedClientHandler(SelectedMicProfile.ConnectedClientId, out IConnectedClientHandler connectedClientHandler))
        {
            return null;
        }

        return connectedClientHandler;
    }

    private void SendSelectedMicProfileToConnectedClient()
    {
        IConnectedClientHandler connectedClientHandler = GetConnectedClientHandler();
        connectedClientHandler?.SendMessageToClient(new MicProfileMessageDto(SelectedMicProfile));
    }

    private void OnRecordingDeviceSelected(MicProfile micProfile)
    {
        if (micProfile == null)
        {
            return;
        }

        amplificationPickerControl.TrySelectItem(micProfile.Amplification);
        noiseSuppressionPickerControl.TrySelectItem(micProfile.NoiseSuppression);
        delayPickerControl.SelectItem(micProfile.DelayInMillis);
        colorPickerControl.TrySelectItem(micProfile.Color);
        sampleRatePickerControl.TrySelectItem(micProfile.SampleRate);

        enabledToggle.value = micProfile.IsEnabled;
        UpdateRecordingDeviceInactiveOverlay();
        
        if (micProfile.IsConnected(serverSideConnectRequestManager))
        {
            notConnectedContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
            deleteButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.None);
        }
        else
        {
            notConnectedContainer.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
            deleteButton.style.display = new StyleEnum<DisplayStyle>(DisplayStyle.Flex);
        }

        micVisualizer.SetMicProfile(micProfile);
        calibrateMicDelayControl.MicProfile = micProfile;
        noteLabel.text = TranslationManager.GetTranslation(R.Messages.options_note, "value", "?");

        UpdateSampleRateLabel();
        InitPitchDetectionFromConnectionClient();
    }

    private void UpdateRecordingDeviceInactiveOverlay()
    {
        recordingDeviceInactiveOverlay.style.backgroundColor = enabledToggle.value
            ? new StyleColor(Color.clear)
            : new StyleColor(new Color(0, 0, 0, 0.5f));
    }

    private void DeleteSelectedRecordingDevice()
    {
        if (SelectedMicProfile == null)
        {
            return;
        }

        if (!SelectedMicProfile.IsConnected(serverSideConnectRequestManager))
        {
            settings.MicProfiles.Remove(SelectedMicProfile);
            devicePickerControl.Items = CreateMicProfiles();
            if (devicePickerControl.Items.Count > 0)
            {
                devicePickerControl.Selection.Value = devicePickerControl.Items[0];
            }
        }
    }

    public void UpdateTranslation()
    {
        deleteButton.text = TranslationManager.GetTranslation(R.Messages.delete);
        colorPicker.Label = TranslationManager.GetTranslation(R.Messages.options_color);
        delayPicker.Label = TranslationManager.GetTranslation(R.Messages.options_delay);
        amplificationPicker.Label = TranslationManager.GetTranslation(R.Messages.options_amplification);
        noiseSuppressionPicker.Label = TranslationManager.GetTranslation(R.Messages.options_noiseSuppression);
        sampleRatePicker.Label = TranslationManager.GetTranslation(R.Messages.options_sampleRate);
        noteLabel.text = TranslationManager.GetTranslation(R.Messages.options_note, "value", "?");
        calibrateDelayButton.text = TranslationManager.GetTranslation(R.Messages.options_delay_calibrate);
        notConnectedLabel.text = TranslationManager.GetTranslation(R.Messages.options_deviceNotConnected);
    }

    private List<MicProfile> CreateMicProfiles()
    {
        // Create list of connected and loaded microphones without duplicates.
        // A loaded microphone might have been created with hardware that is not connected now.
        List<string> connectedMicNames = Microphone.devices.ToList();
        List<MicProfile> loadedMicProfiles = settings.MicProfiles;
        List<MicProfile> micProfiles = new(loadedMicProfiles);
        List<IConnectedClientHandler> connectedClientHandlers = serverSideConnectRequestManager.GetAllConnectedClientHandlers();

        // Create mic profiles for connected microphones that are not yet in the list
        foreach (string connectedMicName in connectedMicNames)
        {
            bool alreadyInList = micProfiles.AnyMatch(it => it.Name == connectedMicName && !it.IsInputFromConnectedClient);
            if (!alreadyInList)
            {
                MicProfile micProfile = new(connectedMicName);
                micProfiles.Add(micProfile);
            }
        }

        // Create mic profiles for connected companion apps that are not yet in the list
        foreach (IConnectedClientHandler connectedClientHandler in connectedClientHandlers)
        {
            bool alreadyInList = micProfiles.AnyMatch(it => it.ConnectedClientId == connectedClientHandler.ClientId && it.IsInputFromConnectedClient);
            if (!alreadyInList)
            {
                MicProfile micProfile = new(connectedClientHandler.ClientName, connectedClientHandler.ClientId);
                micProfiles.Add(micProfile);
            }
        }

        micProfiles.Sort(MicProfile.compareByName);

        return micProfiles;
    }

    public void UpdateMicProfileNames(ClientConnectionEvent clientConnectionEvent)
    {
        if (clientConnectionEvent.IsConnected)
        {
            devicePickerControl.Items.ForEach(micProfile => UpdateMicProfileName(clientConnectionEvent, micProfile));
        }
        devicePickerControl.UpdateLabelText();
    }

    private void UpdateMicProfileName(ClientConnectionEvent clientConnectionEvent, MicProfile micProfile)
    {
        if (micProfile.IsInputFromConnectedClient
            && micProfile.ConnectedClientId == clientConnectionEvent.ConnectedClientHandler.ClientId)
        {
            micProfile.Name = clientConnectionEvent.ConnectedClientHandler.ClientName;

            if (devicePickerControl.SelectedItem == micProfile)
            {
                devicePickerControl.UpdateLabelText();
            }
        }
    }

    public override bool HasHelpDialog => true;
    public override MessageDialogControl CreateHelpDialogControl()
    {
        Dictionary<string, string> titleToContentMap = new()
        {
            { TranslationManager.GetTranslation(R.Messages.options_recording_helpDialog_micDelay_title),
                TranslationManager.GetTranslation(R.Messages.options_recording_helpDialog_micDelay) },
            { TranslationManager.GetTranslation(R.Messages.options_recording_helpDialog_micDelayCalibration_title),
                TranslationManager.GetTranslation(R.Messages.options_recording_helpDialog_micDelayCalibration) },
            { TranslationManager.GetTranslation(R.Messages.options_recording_helpDialog_amplification_title),
                TranslationManager.GetTranslation(R.Messages.options_recording_helpDialog_amplification) },
            { TranslationManager.GetTranslation(R.Messages.options_recording_helpDialog_noiseSuppression_title),
                TranslationManager.GetTranslation(R.Messages.options_recording_helpDialog_noiseSuppression) },
            { TranslationManager.GetTranslation(R.Messages.options_recording_helpDialog_sampleRate_title),
                TranslationManager.GetTranslation(R.Messages.options_recording_helpDialog_sampleRate) },
        };
        MessageDialogControl helpDialogControl = uiManager.CreateHelpDialogControl(
            TranslationManager.GetTranslation(R.Messages.options_recording_helpDialog_title),
            titleToContentMap);
        helpDialogControl.AddButton(TranslationManager.GetTranslation(R.Messages.viewMore),
            _ => Application.OpenURL(TranslationManager.GetTranslation(R.Messages.uri_howToConfigureMicsAndSpeaker)));
        return helpDialogControl;
    }

    private void InitPitchDetectionFromConnectionClient()
    {
        if (connectedClientReceivedMessageStreamDisposable != null)
        {
            connectedClientReceivedMessageStreamDisposable.Dispose();
            connectedClientReceivedMessageStreamDisposable = null;
        }

        if (SelectedMicProfile == null
            || !SelectedMicProfile.IsInputFromConnectedClient
            || !serverSideConnectRequestManager.TryGetConnectedClientHandler(SelectedMicProfile.ConnectedClientId, out IConnectedClientHandler connectedClientHandler))
        {
            return;
        }

        connectedClientReceivedMessageStreamDisposable = connectedClientHandler.ReceivedMessageStream
            .ObserveOnMainThread()
            .Subscribe(dto =>
            {
                if (dto is BeatPitchEventDto beatPitchEventDto)
                {
                    connectedClientBeatPitchEventStream.OnNext(new BeatPitchEvent(beatPitchEventDto.MidiNote, beatPitchEventDto.Beat));
                }
            })
            .AddTo(gameObject);
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(micVisualizer);
        bb.BindExistingInstance(calibrateMicDelayControl);
        return bb.GetBindings();
    }
}
