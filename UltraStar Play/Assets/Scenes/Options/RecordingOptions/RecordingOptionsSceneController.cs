using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using UniInject;


// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class RecordingOptionsSceneController : MonoBehaviour, INeedInjection
{
    public RecordingDeviceSlider recordingDeviceSlider;
    public AmplificationSlider amplificationSlider;
    public NoiseSuppressionSlider noiseSuppressionSlider;
    public MicDelayNumberSpinner delaySpinner;
    public CalibrateMicDelayButton calibrateMicDelayButton;
    public RecordingDeviceColorSlider colorSlider;
    public Toggle enabledToggle;
    public Button deleteButton;

    public GameObject noHardwareWarningAndDeleteButton;

    public RecordingOptionsMicVisualizer micVisualizer;

    private MicProfile SelectedMicProfile => recordingDeviceSlider.SelectedItem;
    
    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    private readonly List<IDisposable> disposables = new List<IDisposable>();
    
    void Start()
    {
        recordingDeviceSlider.Selection.Subscribe(newValue => OnRecordingDeviceSelected(newValue));
        enabledToggle.OnValueChangedAsObservable().Subscribe(newValue => SetSelectedRecordingDeviceEnabled(newValue));
        deleteButton.OnClickAsObservable().Subscribe(_ => DeleteSelectedRecordingDevice());

        recordingDeviceSlider.Selection.Value = recordingDeviceSlider.Items[0];
        
        // Reselect recording device of connected client, when the client has now connected
        disposables.Add(serverSideConnectRequestManager.ClientConnectedEventStream
            .Where(clientConnectedEvent => recordingDeviceSlider.SelectedItem?.ConnectedClientId == clientConnectedEvent.ConnectedClientHandler.ClientId)
            .Subscribe(newValue => OnRecordingDeviceSelected(recordingDeviceSlider.SelectedItem)));
    }

    private void SetSelectedRecordingDeviceEnabled(bool enabled)
    {
        if (SelectedMicProfile == null)
        {
            return;
        }
        SelectedMicProfile.IsEnabled = enabled;
        if (enabled)
        {
            SettingsManager.Instance.Settings.MicProfiles.AddIfNotContains(SelectedMicProfile);
        }
    }

    private void OnRecordingDeviceSelected(MicProfile micProfile)
    {
        if (micProfile == null)
        {
            return;
        }
        amplificationSlider.SetMicProfile(micProfile);
        noiseSuppressionSlider.SetMicProfile(micProfile);
        delaySpinner.SetMicProfile(micProfile);
        colorSlider.SetMicProfile(micProfile);
        calibrateMicDelayButton.MicProfile = micProfile;
        enabledToggle.isOn = micProfile.IsEnabled;

        bool hasNoHardware = !micProfile.IsConnected;
        noHardwareWarningAndDeleteButton.SetActive(hasNoHardware);

        micVisualizer.SetMicProfile(micProfile);
    }

    private void DeleteSelectedRecordingDevice()
    {
        if (SelectedMicProfile == null)
        {
            return;
        }

        if (!SelectedMicProfile.IsConnected)
        {
            SettingsManager.Instance.Settings.MicProfiles.Remove(SelectedMicProfile);
            recordingDeviceSlider.UpdateItems();
            recordingDeviceSlider.Selection.Value = recordingDeviceSlider.Items[0];
        }
    }

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
    }
}
