using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
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

    [Inject(UxmlName = R.UxmlNames.noMicIcon)]
    private VisualElement noMicIcon;
    
    [Inject(UxmlName = R.UxmlNames.nameLabel)]
    private Label nameLabel;
    
    [Inject(UxmlName = R.UxmlNames.playerImage)]
    private VisualElement playerImage;

    [Inject(UxmlName = R.UxmlNames.togglePlayerSelectedButton)]
    private Button togglePlayerSelectedButton;
    
    [Inject(UxmlName = R.UxmlNames.toggleVoiceButton)]
    private Button toggleVoiceButton;
    
    [Inject]
    private Injector injector;
    
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
                micIcon.HideByDisplay();
                noMicIcon.ShowByDisplay();
            }
            else
            {
                micIcon.ShowByDisplay();
                noMicIcon.HideByDisplay();
                micIcon.style.unityBackgroundImageTintColor = new StyleColor(micProfile.Color);
            }

            UpdateMicPitchTracker();
            micProgressBarRecordingControl.MicProfile = MicProfile;
        }
    }

    private readonly ReactiveProperty<string> selectedVoiceName = new(Voice.firstVoiceName);
    public string VoiceName => toggleVoiceButton.IsVisibleByDisplay()
        ? selectedVoiceName.Value
        : null;

    private MicPitchTracker micPitchTracker;

    private readonly MicProgressBarRecordingControl micProgressBarRecordingControl = new();

    public ReactiveProperty<bool> IsSelected {get; private set; } = new(false);

    private Dictionary<string, string> voiceNames;

    public void OnInjectionFinished()
    {
        InitVoiceSelection();

        // Delay mic initialization because it takes some time and scene change should happen fast.
        MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(1f, () =>
        {
            if (!Application.isPlaying)
            {
                return;
            }
            InitMicPitchTracker();
        }));

        injector.Inject(micProgressBarRecordingControl);
        micProgressBarRecordingControl.MicProfile = MicProfile;

        togglePlayerSelectedButton.RegisterCallbackButtonTriggered(() => IsSelected.Value = !IsSelected.Value);
        
        IsSelected.Subscribe(newValue =>
        {
            if (newValue)
            {
                playerImage.style.unityBackgroundImageTintColor = new StyleColor(Colors.white);
                noMicIcon.ShowByVisibility();
            }
            else
            {
                playerImage.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.25f, 0.25f, 0.25f));
                noMicIcon.HideByVisibility();
            }
        });
    }

    private void InitVoiceSelection()
    {
        selectedVoiceName.Subscribe(_ => UpdateToggleVoiceButtonText());
        toggleVoiceButton.RegisterCallbackButtonTriggered(() =>
        {
            selectedVoiceName.Value = selectedVoiceName.Value == Voice.firstVoiceName
                ? Voice.secondVoiceName
                : Voice.firstVoiceName;
        });
    }

    private void UpdateToggleVoiceButtonText()
    {
        if (!voiceNames.IsNullOrEmpty()
            && voiceNames.ContainsKey(selectedVoiceName.Value))
        {
            toggleVoiceButton.text = voiceNames[selectedVoiceName.Value];
        }
        else
        {
            toggleVoiceButton.text = selectedVoiceName.Value;
        }
    }

    public void Init(PlayerProfile playerProfile)
    {
        this.PlayerProfile = playerProfile;
        nameLabel.text = playerProfile.Name;
        injector.WithRootVisualElement(playerImage)
            .WithBindingForInstance(playerProfile)
            .CreateAndInject<PlayerProfileImageControl>();
        MicProfile = null;
    }

    public void HideVoiceSelection()
    {
        toggleVoiceButton.HideByDisplay();
    }

    public void ShowVoiceSelection(SongMeta selectedSong, int selectedVoiceIndex)
    {
        voiceNames = selectedSong.VoiceNames;
        toggleVoiceButton.ShowByDisplay();
        UpdateToggleVoiceButtonText();
        selectedVoiceName.Value = selectedVoiceIndex == 0
            ? Voice.firstVoiceName
            : Voice.secondVoiceName;
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
        if (micPitchTracker == null)
        {
            return;
        }
        
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
