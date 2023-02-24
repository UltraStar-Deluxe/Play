using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class SongSelectPlayerEntryControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    [Inject(Key = nameof(micPitchTrackerPrefab))]
    private MicPitchTracker micPitchTrackerPrefab;
    
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;

    [Inject(UxmlName = R.UxmlNames.micIcon)]
    private VisualElement micIcon;

    [Inject(UxmlName = R.UxmlNames.nameLabel)]
    private Label nameLabel;

    [Inject(UxmlName = R.UxmlNames.enabledToggle)]
    public Toggle EnabledToggle { get; private set; }

    [Inject]
    private Injector injector;
    
    private LabeledItemPickerControl<Voice> voiceChooserControl;

    // The PlayerProfile is set in Init and must not be null.
    public PlayerProfile PlayerProfile { get; private set; }

    // The MicProfile can be null to indicate that this player does not have a mic (yet).
    private MicProfile micProfile;
    public MicProfile MicProfile
    {
        get
        {
            return micProfile;
        }
        set
        {
            micProfile = value;
            if (micProfile == null)
            {
                micIcon.HideByVisibility();
            }
            else
            {
                micIcon.ShowByVisibility();
                micIcon.style.unityBackgroundImageTintColor = new StyleColor(micProfile.Color);
            }

            UpdateMicPitchTracker();
            micProgressBarRecordingControl.MicProfile = MicProfile;
        }
    }

    public Voice Voice => voiceChooserControl.ItemPicker.IsVisibleByDisplay()
        ? voiceChooserControl.Selection.Value
        : null;

    private MicPitchTracker micPitchTracker;

    private readonly MicProgressBarRecordingControl micProgressBarRecordingControl = new();

    public bool IsSelected
    {
        get
        {
            return EnabledToggle.value;
        }
    }

    public void OnInjectionFinished()
    {
        voiceChooserControl = new LabeledItemPickerControl<Voice>(visualElement.Q<ItemPicker>(R.UxmlNames.voiceChooser), new List<Voice>());
        voiceChooserControl.GetLabelTextFunction = voice => voice != null
            ? voice.Name
            : "";
        
        InitMicPitchTracker();

        injector.Inject(micProgressBarRecordingControl);
        micProgressBarRecordingControl.MicProfile = MicProfile;
    }

    public void SetSelected(bool newIsSelected)
    {
        EnabledToggle.value = newIsSelected;
    }

    public void Init(PlayerProfile playerProfile)
    {
        this.PlayerProfile = playerProfile;
        nameLabel.text = playerProfile.Name;
        MicProfile = null;
    }

    public void HideVoiceSelection()
    {
        voiceChooserControl.SelectItem(null);
        voiceChooserControl.ItemPicker.HideByDisplay();
    }

    public void ShowVoiceSelection(SongMeta selectedSong, int selectedVoiceIndex)
    {
        voiceChooserControl.Items = selectedSong.GetVoices()
            .ToList();
        voiceChooserControl.ItemPicker.ShowByDisplay();
        voiceChooserControl.SelectItem(voiceChooserControl.Items[selectedVoiceIndex]);

        voiceChooserControl.GetLabelTextFunction = voice => voice != null
            ? selectedSong.VoiceNames[voice.Name]
            : "";
    }

    private void InitMicPitchTracker()
    {
        micPitchTracker = GameObject.Instantiate(micPitchTrackerPrefab);
        injector.InjectAllComponentsInChildren(micPitchTracker);
        micPitchTracker.MicProfile = micProfile;
        UpdateMicPitchTracker();
    }
    
    private void UpdateMicPitchTracker()
    {
        micPitchTracker.MicProfile = micProfile;
        if (micProfile == null
            || micProfile.IsInputFromConnectedClient)
        {
            if (micPitchTracker.MicSampleRecorder.IsRecording.Value)
            {
                micPitchTracker.MicSampleRecorder.StopRecording();
            }
        }
        else
        {
            if (!micPitchTracker.MicSampleRecorder.IsRecording.Value)
            {
                micPitchTracker.MicSampleRecorder.StartRecording();
            }
        }
    }

    public void Dispose()
    {
        GameObject.Destroy(micPitchTracker);
    }
}
