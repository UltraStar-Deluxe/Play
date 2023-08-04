using System;
using System.Collections.Generic;
using System.Linq;
using PortAudioForUnity;
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
    private static readonly List<int> noiseSuppressionItems= new() { 0, 1, 3, 5, 10, 15, 20, 25, 30 };

    [InjectedInInspector]
    public RecordingOptionsMicVisualizer micVisualizer;

    [InjectedInInspector]
    public CalibrateMicDelayControl calibrateMicDelayControl;

    [InjectedInInspector]
    public NewestSamplesMicPitchTracker micPitchTracker;

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
    
    [Inject]
    private MicSampleRecorderManager micSampleRecorderManager;

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
    
    [Inject(UxmlName = R.UxmlNames.usePortAudioToggle)]
    private Toggle usePortAudioToggle;

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
    
    // [Inject(UxmlName = R.UxmlNames.micPlaybackVolumeChooser)]
    // private ItemPicker micPlaybackVolumeChooser;

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
        
        devicePickerControl = new LabeledItemPickerControl<MicProfile>(devicePicker, CreateAndPersistMicProfiles());
        devicePickerControl.AutoSmallFont = false;
        devicePickerControl.GetLabelTextFunction = item => item != null ? item.GetDisplayNameWithChannel() : "";
        if (!TryReSelectLastMicProfile())
        {
            devicePickerControl.Selection.Value = devicePickerControl.Items[0];
        }
        devicePickerControl.Selection.Subscribe(micProfile =>
            {
                if (micProfile == null)
                {
                    return;
                }

                settings.LastMicProfileNameInRecordingOptionsScene = micProfile.Name;
                settings.LastMicProfileChannelIndexInRecordingOptionsScene = micProfile.ChannelIndex;
            });

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
        
        // Select random color via context menu
        VisualElement colorPickerColorElement = colorPicker.Q<VisualElement>(null, R_PlayShared.UssClasses.itemPickerItemLabel);
        if (colorPickerColorElement != null)
        {
            ContextMenuControl contextMenuControl = injector
                .WithRootVisualElement(colorPickerColorElement)
                .CreateAndInject<ContextMenuControl>();
            contextMenuControl.FillContextMenuAction = contextMenuPopupControl =>
            {
                contextMenuPopupControl.AddButton("Random Color", () =>
                {
                    colorPickerControl.SelectItem(Colors.CreateRandomColor());
                });
            };
        }

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
        micPitchTracker.FinalSampleRate
            .Subscribe(_ => UpdateSampleRateLabel())
            .AddTo(gameObject);
        micPitchTracker.IsRecording
            .Subscribe(_ => UpdateSampleRateLabel())
            .AddTo(gameObject);

        // Update recording device of connected client, when the client (dis)connects
        serverSideConnectRequestManager.ClientConnectionChangedEventStream
            .Where(clientConnectedEvent => devicePickerControl.SelectedItem?.ConnectedClientId == clientConnectedEvent.ConnectedClientHandler.ClientId)
            .Subscribe(newValue => OnRecordingDeviceSelected(devicePickerControl.SelectedItem))
            .AddTo(gameObject);

        serverSideConnectRequestManager.ConnectedClientMicProfileChangedEventStream
            .Subscribe(OnConnectedClientMicProfileChanged)
            .AddTo(gameObject);

        micSampleRecorderManager.ConnectedMicDevicesChangesStream
            .Subscribe(evt => OnConnectedMicDevicesChanged())
            .AddTo(gameObject);

        calibrateDelayButton.RegisterCallbackButtonTriggered(_ => calibrateMicDelayControl.StartCalibration());
        calibrateMicDelayControl.CalibrationResultEventStream
            .ObserveOnMainThread()
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

        // Use PortAudio
        if (ApplicationUtils.CanUsePortAudio())
        {
            FieldBindingUtils.Bind(gameObject, usePortAudioToggle,
                () => settings.PreferPortAudio,
                preferPortAudio =>
                {
                    micPitchTracker.StopRecording();

                    settings.PreferPortAudio = preferPortAudio;
                    ApplicationUtils.SetUsePortAudio(preferPortAudio);

                    Debug.Log($"UsePortAudio: {MicrophoneAdapter.UsePortAudio}");

                    UpdateRecordingDevices();
                });
        }
        else
        {
            usePortAudioToggle.HideByDisplay();
        }
    }

    private void OnConnectedMicDevicesChanged()
    {
        UpdateRecordingDevices();
    }

    private void UpdateRecordingDevices()
    {
        micPitchTracker.StopRecording();

        MicProfile lastMicProfile = SelectedMicProfile;
        devicePickerControl.Items = CreateAndPersistMicProfiles();
        Debug.Log($"MicProfiles: {devicePickerControl.Items.ToCsv()}");
        if (devicePickerControl.Items.Count > 0)
        {
            MicProfile nextSelectedMicProfile = devicePickerControl.Items[0];

            // Try to restore selection
            if (lastMicProfile != null)
            {
                MicProfile matchingMicProfile = devicePickerControl.Items.FirstOrDefault(micProfile =>
                    micProfile.Name == lastMicProfile.Name
                    && micProfile.ChannelIndex == lastMicProfile.ChannelIndex);
                if (matchingMicProfile != null)
                {
                    nextSelectedMicProfile = matchingMicProfile;
                }
            }

            devicePickerControl.Selection.Value = nextSelectedMicProfile;
            devicePickerControl.UpdateLabelText();

            OnRecordingDeviceSelected(nextSelectedMicProfile);
        }
    }

    // private void Update()
    // {
    //     // Read messages from client since last time the reader thread was active.
    //     IConnectedClientHandler connectedClientHandler = GetConnectedClientHandler();
    //     connectedClientHandler?.ReadMessagesFromClient();
    // }

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
                : $"\n({micPitchTracker.FinalSampleRate.Value} Hz)";
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
            .FirstOrDefault(micProfile => micProfile.Name == settings.LastMicProfileNameInRecordingOptionsScene 
                                          && micProfile.ChannelIndex == settings.LastMicProfileChannelIndexInRecordingOptionsScene);
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

        micPitchTracker.MicProfile = micProfile;
        amplificationPickerControl.TrySelectItem(micProfile.Amplification);
        noiseSuppressionPickerControl.TrySelectItem(micProfile.NoiseSuppression);
        delayPickerControl.SelectItem(micProfile.DelayInMillis);
    
        Color32 micProfileColor = micProfile.Color
            .OrIfDefault(colorPickerControl.Items.FirstOrDefault());
        if (colorPickerControl.Items.Contains(micProfileColor))
        {
            colorPickerControl.TrySelectItem(micProfileColor);
        }
        else
        {
            colorPickerControl.SelectItem(micProfile.Color);
        }

        sampleRatePickerControl.TrySelectItem(micProfile.SampleRate);

        enabledToggle.value = micProfile.IsEnabled;
        UpdateRecordingDeviceInactiveOverlay();

        bool isConnected = micProfile.IsConnected(serverSideConnectRequestManager);
        notConnectedContainer.SetVisibleByDisplay(!isConnected);
        deleteButton.SetVisibleByDisplay(!isConnected);

        micVisualizer.SetMicProfile(micProfile);
        noteLabel.text = TranslationManager.GetTranslation(R.Messages.options_note, "value", "?");
        
        // playRecordedAudioInfoContainer.SetVisibleByDisplay(micProfile.IsInputFromConnectedClient);
        
        UpdateSampleRateLabel();
        InitPitchDetectionFromConnectionClient();
    }

    private void UpdateRecordingDeviceInactiveOverlay()
    {
        recordingDeviceInactiveOverlay.ShowByDisplay();
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
            UpdateRecordingDevices();
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

    private List<MicProfile> CreateAndPersistMicProfiles()
    {
        return MicProfileUtils.CreateAndPersistMicProfiles(
            settings,
            themeManager,
            serverSideConnectRequestManager);
    }

    private void OnConnectedClientMicProfileChanged(MicProfile micProfile)
    {
        if (devicePickerControl.SelectedItem == micProfile)
        {
            devicePickerControl.UpdateLabelText();
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
            .Subscribe(dto =>
            {
                if (dto is BeatPitchEventDto beatPitchEventDto)
                {
                    FireBeatPitchEvent(beatPitchEventDto);
                }
                else if (dto is BeatPitchEventsDto beatPitchEventsDto)
                {
                    beatPitchEventsDto.BeatPitchEvents.ForEach(beatPitchEventDto => FireBeatPitchEvent(beatPitchEventDto));
                }
            })
            .AddTo(gameObject);
    }

    private void FireBeatPitchEvent(BeatPitchEventDto beatPitchEventDto)
    {
        connectedClientBeatPitchEventStream.OnNext(new BeatPitchEvent(beatPitchEventDto.MidiNote, beatPitchEventDto.Beat, beatPitchEventDto.Frequency));
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(micVisualizer);
        bb.BindExistingInstance(micPitchTracker);
        bb.BindExistingInstance(calibrateMicDelayControl);
        return bb.GetBindings();
    }
}
