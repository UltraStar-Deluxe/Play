using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class RecordingOptionsSceneControl : AbstractOptionsSceneControl, IBinder
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
    private ServerSideCompanionClientManager serverSideCompanionClientManager;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private MicSampleRecorderManager micSampleRecorderManager;

    [Inject(UxmlName = R.UxmlNames.deviceChooser)]
    private DropdownField deviceChooser;

    [Inject(UxmlName = R.UxmlNames.amplificationChooser)]
    private Chooser amplificationChooser;

    [Inject(UxmlName = R.UxmlNames.noiseSuppressionChooser)]
    private Chooser noiseSuppressionChooser;

    [Inject(UxmlName = R.UxmlNames.delayChooser)]
    private Chooser delayChooser;

    [Inject(UxmlName = R.UxmlNames.colorChooser)]
    private Chooser colorChooser;

    [Inject(UxmlName = R.UxmlNames.sampleRateChooser)]
    private Chooser sampleRateChooser;

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

    [Inject(UxmlName = R.UxmlNames.noConnectedMicsContainer)]
    private VisualElement noConnectedMicsContainer;

    [Inject(UxmlName = R.UxmlNames.micConfigurationContainer)]
    private VisualElement micConfigurationContainer;

    [Inject(UxmlName = R.UxmlNames.playRecordedAudioToggle)]
    private Toggle playRecordedAudioToggle;

    [Inject(UxmlName = R.UxmlNames.playRecordedAudioInfoContainer)]
    private VisualElement playRecordedAudioInfoContainer;

    [Inject(UxmlName = R.UxmlNames.micPlaybackVolumeChooser)]
    private Chooser micPlaybackVolumeChooser;

    [Inject(UxmlName = R.UxmlNames.systemAudioBackendDelayChooser)]
    private Chooser systemAudioBackendDelayChooser;

    private SampleRateChooserControl sampleRateChooserControl;
    private DropdownFieldControl<MicProfile> deviceChooserControl;
    private LabeledChooserControl<int> amplificationChooserControl;
    private LabeledChooserControl<int> noiseSuppressionChooserControl;
    private NumberChooserControl delayChooserControl;
    private ColorChooserControl colorChooserControl;

    private MicProfile SelectedMicProfile => deviceChooserControl.Selection;

    private IDisposable companionClientReceivedMessageStreamDisposable;

    private readonly Subject<BeatPitchEvent> companionClientBeatPitchEventStream = new();
    public IObservable<BeatPitchEvent> CompanionClientBeatPitchEventStream => companionClientBeatPitchEventStream;

    protected override void Start()
    {
        base.Start();

        List<MicProfile> micProfiles = CreateAndPersistMicProfiles();
        UpdateNoConnectedMicsContainer(micProfiles);

        // new AutoFitLabelControl(deviceChooser.Q<Label>(null, "unity-base-popup-file__text"), 10, 15);

        deviceChooserControl = new DropdownFieldControl<MicProfile>(
            deviceChooser,
            micProfiles,
            GetInitialRecordingDeviceSelection(micProfiles),
            GetRecordingDeviceDisplayText);
        deviceChooserControl.SelectionAsObservable.Subscribe(micProfile =>
            {
                if (micProfile == null)
                {
                    return;
                }

                settings.LastMicProfileNameInRecordingOptionsScene = micProfile.Name;
                settings.LastMicProfileChannelIndexInRecordingOptionsScene = micProfile.ChannelIndex;
            });

        amplificationChooserControl = new LabeledChooserControl<int>(amplificationChooser, amplificationItems, item => Translation.Of(item + " %"));
        noiseSuppressionChooserControl = new LabeledChooserControl<int>(noiseSuppressionChooser, noiseSuppressionItems, item => Translation.Of(item + " %"));
        delayChooserControl = new NumberChooserControl(delayChooser);
        delayChooserControl.GetLabelTextFunction = item => item + " ms";
        colorChooserControl = new ColorChooserControl(colorChooser, themeManager.GetMicrophoneColors());
        sampleRateChooserControl = new SampleRateChooserControl(sampleRateChooser, item => GetSampleRateLabel(item));
        enabledToggle.RegisterValueChangedCallback(evt => SetSelectedRecordingDeviceEnabled(evt.newValue));
        deleteButton.RegisterCallbackButtonTriggered(_ => DeleteSelectedRecordingDevice());

        // Select random color via context menu
        VisualElement colorChooserColorElement = colorChooser.Q<VisualElement>(null, R_PlayShared.UssClasses.chooserItemLabel);
        if (colorChooserColorElement != null)
        {
            ContextMenuControl contextMenuControl = injector
                .WithRootVisualElement(colorChooserColorElement)
                .CreateAndInject<ContextMenuControl>();
            contextMenuControl.FillContextMenuAction = contextMenuPopupControl =>
            {
                contextMenuPopupControl.AddButton(Translation.Get(R.Messages.options_recording_action_selectRandomColor),
                    () => colorChooserControl.Selection = Colors.CreateRandomColor());
            };
        }

        deviceChooserControl.SelectionAsObservable.Subscribe(newValue => OnRecordingDeviceSelected(newValue));
        amplificationChooserControl.SelectionAsObservable.Subscribe(newValue =>
        {
            if (SelectedMicProfile == null)
            {
                return;
            }
            SelectedMicProfile.Amplification = newValue;
            SendSelectedMicProfileToCompanionClient();
        });
        noiseSuppressionChooserControl.SelectionAsObservable.Subscribe(newValue =>
        {
            if (SelectedMicProfile == null)
            {
                return;
            }
            SelectedMicProfile.NoiseSuppression = newValue;
            SendSelectedMicProfileToCompanionClient();
        });
        delayChooserControl.SelectionAsObservable.Subscribe(newValue =>
        {
            if (SelectedMicProfile == null)
            {
                return;
            }
            SelectedMicProfile.DelayInMillis = (int)newValue;
            SendSelectedMicProfileToCompanionClient();
        });
        colorChooserControl.SelectionAsObservable.Subscribe(newValue =>
        {
            if (SelectedMicProfile == null)
            {
                return;
            }
            SelectedMicProfile.Color = newValue;
            SendSelectedMicProfileToCompanionClient();
        });
        sampleRateChooserControl.SelectionAsObservable.Subscribe(newValue =>
        {
            if (SelectedMicProfile == null)
            {
                return;
            }
            SelectedMicProfile.SampleRate = newValue;
            SendSelectedMicProfileToCompanionClient();
        });
        micPitchTracker.FinalSampleRate
            .Subscribe(_ => UpdateSampleRateLabel())
            .AddTo(gameObject);
        micPitchTracker.IsRecording
            .Subscribe(_ => UpdateSampleRateLabel())
            .AddTo(gameObject);

        // Update recording device of connected client, when the client (dis)connects
        serverSideCompanionClientManager.ClientConnectionChangedEventStream
            .Where(clientConnectedEvent => deviceChooserControl.Selection?.ConnectedClientId == clientConnectedEvent.CompanionClientHandler.ClientId)
            .Subscribe(newValue => OnRecordingDeviceSelected(deviceChooserControl.Selection))
            .AddTo(gameObject);

        serverSideCompanionClientManager.CompanionClientMicProfileChangedEventStream
            .Subscribe(OnCompanionClientMicProfileChanged)
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
                    double roundedMedianValue = ((int)(medianValue / delayChooserControl.StepValue)) * delayChooserControl.StepValue;
                    delayChooserControl.Selection = roundedMedianValue;
                }
                else
                {
                    NotificationManager.CreateNotification(Translation.Get(R.Messages.options_delay_calibrate_timeout));
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

                    Debug.Log($"UsePortAudio: {IMicrophoneAdapter.Instance.UsePortAudio}");

                    UpdateRecordingDevices();
                });
        }
        else
        {
            usePortAudioToggle.HideByDisplay();
        }

        // Play recorded audio
        FieldBindingUtils.Bind(gameObject, playRecordedAudioToggle,
            () => settings.PlayRecordedAudio,
            newValue => settings.PlayRecordedAudio = newValue);

        PercentNumberChooserControl micPlaybackVolumeChooserControl = new(micPlaybackVolumeChooser);
        micPlaybackVolumeChooserControl.Bind(
            () => settings.MicrophonePlaybackVolumePercent,
            newValue => settings.MicrophonePlaybackVolumePercent = (int)newValue);

        settings.ObserveEveryValueChanged(it => it.PlayRecordedAudio)
            .Subscribe(newValue => micPlaybackVolumeChooser.SetVisibleByDisplay(newValue))
            .AddTo(gameObject);

        // System audio backend delay
        UnitNumberChooserControl systemAudioBackendDelayChooserControl = new(systemAudioBackendDelayChooser, "ms");
        systemAudioBackendDelayChooserControl.Bind(
            () => settings.SystemAudioBackendDelayInMillis,
            newValue => settings.SystemAudioBackendDelayInMillis = (int)newValue);
    }

    private void OnConnectedMicDevicesChanged()
    {
        UpdateRecordingDevices();
    }

    private void UpdateRecordingDevices()
    {
        micPitchTracker.StopRecording();

        List<MicProfile> micProfiles = CreateAndPersistMicProfiles();
        MicProfile lastMicProfile = SelectedMicProfile;
        deviceChooserControl.Items = micProfiles;
        Debug.Log($"MicProfiles: {deviceChooserControl.Items.JoinWith(", ")}");
        if (deviceChooserControl.Items.Count > 0)
        {
            MicProfile nextSelectedMicProfile = deviceChooserControl.Items[0];

            // Try to restore selection
            if (lastMicProfile != null)
            {
                MicProfile matchingMicProfile = deviceChooserControl.Items.FirstOrDefault(micProfile =>
                    micProfile.Name == lastMicProfile.Name
                    && micProfile.ChannelIndex == lastMicProfile.ChannelIndex);
                if (matchingMicProfile != null)
                {
                    nextSelectedMicProfile = matchingMicProfile;
                }
            }

            deviceChooserControl.Selection = nextSelectedMicProfile;
            deviceChooserControl.UpdateLabelText();

            OnRecordingDeviceSelected(nextSelectedMicProfile);
        }

        UpdateNoConnectedMicsContainer(micProfiles);
    }

    private void UpdateNoConnectedMicsContainer(List<MicProfile> micProfiles)
    {
        bool noMics = micProfiles.IsNullOrEmpty();
        noConnectedMicsContainer.SetVisibleByDisplay(noMics);
        micConfigurationContainer.SetVisibleByDisplay(!noMics);

        if (noMics)
        {
            HighlightHelpIcon();
        }
    }

    private void UpdateSampleRateLabel()
    {
        sampleRateChooserControl.UpdateLabelText();
    }

    private Translation GetSampleRateLabel(int item)
    {
        if (SelectedMicProfile == null)
        {
            return Translation.Empty;
        }

        if (item <= 0)
        {
            // When "auto" is selected, then also show the automatically used sample rate.
            string sampleRateSuffix = SelectedMicProfile.IsInputFromConnectedClient
                ? ""
                : $"\n({micPitchTracker.FinalSampleRate.Value} Hz)";
            return Translation.Of(Translation.Get(R.Messages.options_sampleRate_auto) + sampleRateSuffix);
        }
        return Translation.Of($"{item} Hz");
    }

    private string GetRecordingDeviceDisplayText(MicProfile item)
    {
        return item != null
            ? item.GetDisplayNameWithChannel()
            : "";
    }

    private MicProfile GetInitialRecordingDeviceSelection(List<MicProfile> micProfiles)
    {
        if (settings.LastMicProfileNameInRecordingOptionsScene.IsNullOrEmpty())
        {
            return micProfiles.FirstOrDefault();
        }

        MicProfile lastMicProfile = micProfiles
            .FirstOrDefault(micProfile => micProfile.Name == settings.LastMicProfileNameInRecordingOptionsScene
                                          && micProfile.ChannelIndex == settings.LastMicProfileChannelIndexInRecordingOptionsScene);
        if (lastMicProfile == null)
        {
            return micProfiles.FirstOrDefault();
        }

        return lastMicProfile;
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
            if (!micPitchTracker.IsRecording.Value)
            {
                micPitchTracker.StartRecording();
            }
        }
        else
        {
            if (micPitchTracker.IsRecording.Value)
            {
                micPitchTracker.StopRecording();
            }
        }

        UpdateRecordingDeviceInactiveOverlay();
    }

    private ICompanionClientHandler GetCompanionClientHandler()
    {
        if (SelectedMicProfile == null
            || SelectedMicProfile.ConnectedClientId.IsNullOrEmpty()
            || !serverSideCompanionClientManager.TryGet(SelectedMicProfile.ConnectedClientId, out ICompanionClientHandler companionClientHandler))
        {
            return null;
        }

        return companionClientHandler;
    }

    private void SendSelectedMicProfileToCompanionClient()
    {
        ICompanionClientHandler companionClientHandler = GetCompanionClientHandler();
        companionClientHandler?.SendMessageToClient(new MicProfileMessageDto(SelectedMicProfile));
    }

    private void OnRecordingDeviceSelected(MicProfile micProfile)
    {
        if (micProfile == null)
        {
            return;
        }

        micPitchTracker.MicProfile = micProfile;
        amplificationChooserControl.TrySetSelection(micProfile.Amplification);
        noiseSuppressionChooserControl.TrySetSelection(micProfile.NoiseSuppression);
        delayChooserControl.Selection = micProfile.DelayInMillis;

        Color32 micProfileColor = micProfile.Color
            .OrIfDefault(colorChooserControl.Items.FirstOrDefault());
        if (colorChooserControl.Items.Contains(micProfileColor))
        {
            colorChooserControl.TrySetSelection(micProfileColor);
        }
        else
        {
            colorChooserControl.Selection = micProfile.Color;
        }

        sampleRateChooserControl.TrySetSelection(micProfile.SampleRate);

        enabledToggle.value = micProfile.IsEnabled;
        UpdateRecordingDeviceInactiveOverlay();

        bool isConnected = micProfile.IsConnected(serverSideCompanionClientManager);
        notConnectedContainer.SetVisibleByDisplay(!isConnected);
        deleteButton.SetVisibleByDisplay(!isConnected);

        micVisualizer.SetMicProfile(micProfile);
        noteLabel.SetTranslatedText(Translation.Get(R.Messages.options_note, "value", "?"));

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

        if (!SelectedMicProfile.IsConnected(serverSideCompanionClientManager))
        {
            MicProfile nextSelection = settings.MicProfiles.GetElementBefore(SelectedMicProfile, false);
            nextSelection ??= settings.MicProfiles.GetElementAfter(SelectedMicProfile, false);

            settings.MicProfiles.Remove(SelectedMicProfile);
            UpdateRecordingDevices();

            deviceChooserControl.Selection = nextSelection;
        }
    }

    private List<MicProfile> CreateAndPersistMicProfiles()
    {
        return MicProfileUtils.CreateAndPersistMicProfiles(
            settings,
            themeManager,
            serverSideCompanionClientManager);
    }

    private void OnCompanionClientMicProfileChanged(MicProfile micProfile)
    {
        if (deviceChooserControl.Selection == micProfile)
        {
            deviceChooserControl.UpdateLabelText();
        }
    }

    public override string HelpUri => Translation.Get(R.Messages.uri_howToConfigureMicsAndSpeaker);

    private void InitPitchDetectionFromConnectionClient()
    {
        if (companionClientReceivedMessageStreamDisposable != null)
        {
            companionClientReceivedMessageStreamDisposable.Dispose();
            companionClientReceivedMessageStreamDisposable = null;
        }

        if (SelectedMicProfile == null
            || !SelectedMicProfile.IsInputFromConnectedClient
            || !serverSideCompanionClientManager.TryGet(SelectedMicProfile.ConnectedClientId, out ICompanionClientHandler companionClientHandler))
        {
            return;
        }

        companionClientReceivedMessageStreamDisposable = companionClientHandler.ReceivedMessageStream
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
        companionClientBeatPitchEventStream.OnNext(new BeatPitchEvent(beatPitchEventDto.MidiNote, beatPitchEventDto.Beat, beatPitchEventDto.Frequency));
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
