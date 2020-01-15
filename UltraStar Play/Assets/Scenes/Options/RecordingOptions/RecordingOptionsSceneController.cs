using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;

public class RecordingOptionsSceneController : MonoBehaviour
{
    public RecordingDeviceSlider recordingDeviceSlider;
    public AmplificationSlider amplificationSlider;
    public NoiseSuppressionSlider noiseSuppressionSlider;
    public MicDelaySlider delaySlider;
    public RecordingDeviceColorSlider colorSlider;
    public Toggle enabledToggle;
    public Button deleteButton;

    public GameObject noHardwareWarningAndDeleteButton;

    public RecordingOptionsMicVisualizer micVisualizer;

    void Start()
    {
        recordingDeviceSlider.Selection.Subscribe(newValue => OnRecordingDeviceSelected(newValue));
        enabledToggle.OnValueChangedAsObservable().Subscribe(newValue => SetSelectedRecordingDeviceEnabled(newValue));
        deleteButton.OnClickAsObservable().Subscribe(_ => DeleteSelectedRecordingDevice());

        recordingDeviceSlider.Selection.Value = recordingDeviceSlider.Items[0];
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
        delaySlider.SetMicProfile(micProfile);
        colorSlider.SetMicProfile(micProfile);
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

    private MicProfile SelectedMicProfile
    {
        get
        {
            return recordingDeviceSlider.SelectedItem;
        }
    }
}
