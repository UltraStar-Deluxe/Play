using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class MicrophoneUiControl : INeedInjection, IInjectionFinishedListener
{
    private const int AudioWaveFormTextureWidth = 64;
    private const int AudioWaveFormTextureHeight = 64;

    [Inject]
    private Settings settings;

    [Inject]
    private ClientSideCompanionClientManager clientSideCompanionClientManager;

    [Inject]
    private ClientSideMicDataSender clientSideMicDataSender;

    [Inject]
    private GameObject gameObject;

    [Inject]
    private MicSampleRecorderManager micSampleRecorderManager;

    [Inject(UxmlName = R.UxmlNames.toggleRecordingButton)]
    private Button toggleRecordingButton;

    [Inject(UxmlName = R.UxmlNames.recordingDeviceInfo)]
    private Label recordingDeviceInfo;

    [Inject(UxmlName = R.UxmlNames.clientNameTextField)]
    private TextField clientNameTextField;

    [Inject(UxmlName = R.UxmlNames.visualizeAudioToggle)]
    private Toggle visualizeAudioToggle;

    [Inject(UxmlName = R.UxmlNames.audioWaveForm)]
    private VisualElement audioWaveForm;

    [Inject(UxmlName = R.UxmlNames.connectionTroubleshootingAlert)]
    private VisualElement connectionTroubleshootingAlert;

    [Inject(UxmlName = R.UxmlNames.connectionThroubleshootingText)]
    private Label connectionThroubleshootingText;

    [Inject(UxmlName = R.UxmlNames.serverErrorAlert)]
    private VisualElement serverErrorAlert;

    [Inject(UxmlName = R.UxmlNames.serverErrorResponseText)]
    private Label serverErrorResponseText;

    [Inject(UxmlName = R.UxmlNames.recordingDeviceColorIndicator)]
    private VisualElement recordingDeviceColorIndicator;

    [Inject(UxmlName = R.UxmlNames.noMicrophoneAlert)]
    private VisualElement noMicrophoneAlert;

    [Inject(UxmlName = R.UxmlNames.noMicrophoneText)]
    private Label noMicrophoneText;

    private AudioWaveFormVisualization audioWaveFormVisualization;

    public void OnInjectionFinished()
    {
        clientNameTextField.value = settings.ClientName;
        clientNameTextField.RegisterCallback<NavigationSubmitEvent>(_ => OnClientNameTextFieldSubmit());
        clientNameTextField.RegisterCallback<BlurEvent>(_ => OnClientNameTextFieldSubmit());

        toggleRecordingButton.RegisterCallbackButtonTriggered(_ => ToggleRecording());

        visualizeAudioToggle.value = settings.ShowAudioWaveForm;
        audioWaveForm.SetVisibleByVisibility(settings.ShowAudioWaveForm);
        visualizeAudioToggle.RegisterValueChangedCallback(changeEvent =>
        {
            audioWaveForm.SetVisibleByVisibility(changeEvent.newValue);
            settings.ShowAudioWaveForm = changeEvent.newValue;
        });

        clientSideMicDataSender.IsRecording.Subscribe(OnRecordingStateChanged);
        clientSideMicDataSender.FinalSampleRate.Subscribe(_ => UpdateRecordingDeviceInfo());

        clientSideCompanionClientManager.ConnectEventStream
            .Subscribe(UpdateConnectionStatus);

        audioWaveForm.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            audioWaveFormVisualization = new AudioWaveFormVisualization(
                gameObject,
                audioWaveForm,
                AudioWaveFormTextureWidth,
                AudioWaveFormTextureHeight,
                "main scene audio wave form visualization");
        });

        settings.ObserveEveryValueChanged(it => it.MicProfile)
            .Subscribe(_ => UpdateRecordingDeviceInfo());

        clientSideMicDataSender.MicProfileChangedEventStream
            .Subscribe(newMicProfile =>
            {
                if (newMicProfile == null)
                {
                    return;
                }

                settings.MicProfile = newMicProfile;
                UpdateRecordingDeviceInfo();
            });

        UpdateNoMicrophoneAlert();
        micSampleRecorderManager.ConnectedMicDevicesChangesStream.Subscribe(_ => UpdateNoMicrophoneAlert());

        UpdateTranslation();
    }

    public void Update()
    {
        if (audioWaveForm.IsVisibleByDisplay()
            && audioWaveForm.IsVisibleByVisibility()
            && audioWaveFormVisualization != null)
        {
            audioWaveFormVisualization.DrawAudioWaveForm(clientSideMicDataSender.MicSamples);
        }
    }

    private void UpdateNoMicrophoneAlert()
    {
        bool hasMicrophone = Microphone.devices.Length > 0;
        Log.Debug(() => $"{nameof(UpdateNoMicrophoneAlert)} - hasMicrophone: {hasMicrophone}, devices {Microphone.devices.JoinWith(",")}");

        toggleRecordingButton.SetVisibleByDisplay(hasMicrophone);
        visualizeAudioToggle.SetVisibleByDisplay(hasMicrophone);
        noMicrophoneAlert.SetVisibleByDisplay(!hasMicrophone);
    }

    private void UpdateTranslation()
    {
        visualizeAudioToggle.label = Translation.Get(R.Messages.companionApp_visualizeMicInput);
        noMicrophoneText.text = Translation.Get(R.Messages.companionApp_noMicrophoneHints);
    }

    private void UpdateRecordingDeviceInfo()
    {
        recordingDeviceInfo.text = $"Sample Rate:{clientSideMicDataSender.FinalSampleRate}Hz, " +
                                   $"Delay: {settings.MicProfile.DelayInMillis}ms, " +
                                   $"Amp: {settings.MicProfile.Amplification}, " +
                                   $"Supp: {settings.MicProfile.NoiseSuppression}";
        recordingDeviceColorIndicator.style.backgroundColor = new StyleColor(settings.MicProfile.Color);
    }

    private void ToggleRecording()
    {
        if (clientSideMicDataSender.IsRecording.Value)
        {
            clientSideMicDataSender.StopRecording();
        }
        else
        {
            clientSideMicDataSender.StartRecording();
        }
    }

    private void UpdateConnectionStatus(ConnectEvent connectEvent)
    {
        if (connectEvent.IsSuccess)
        {
            audioWaveForm.SetVisibleByVisibility(settings.ShowAudioWaveForm);
            toggleRecordingButton.Focus();
        }
    }

    private void OnRecordingStateChanged(bool isRecording)
    {
        if (isRecording)
        {
            toggleRecordingButton.AddToClassList("stopRecordingButton");

            // Prevent stand-by when recording
            Debug.Log("Setting Screen.sleepTimeout to SleepTimeout.NeverSleep");
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
        else
        {
            toggleRecordingButton.RemoveFromClassList("stopRecordingButton");

            // Reset stand-by behavior
            Debug.Log("Setting Screen.sleepTimeout to SleepTimeout.SystemSetting");
            Screen.sleepTimeout = SleepTimeout.SystemSetting;
        }
    }

    private void OnClientNameTextFieldSubmit()
    {
        if (clientNameTextField.value == settings.ClientName)
        {
            return;
        }

        if (clientNameTextField.value.IsNullOrEmpty()
            && !settings.ClientName.IsNullOrEmpty())
        {
            // ClientName must not be empty, so restore last value.
            clientNameTextField.value = settings.ClientName;
        }
        else
        {
            // Apply new ClientName
            settings.ClientName = clientNameTextField.value;
            // Reconnect to let the main know about the new clientName.
            clientSideCompanionClientManager.DisconnectFromServer();
        }
    }
}
