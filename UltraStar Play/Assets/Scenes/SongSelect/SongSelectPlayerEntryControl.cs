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

    [Inject(Key = nameof(micProfiles))]
    private List<MicProfile> micProfiles;
        
    [Inject(Key = nameof(messageDialogUi))]
    private VisualTreeAsset messageDialogUi;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.micButton)]
    private Button micButton;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.micIcon)]
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
    
    [Inject]
    private Settings settings;
    
    [Inject]
    private FocusableNavigator focusableNavigator;
    
    // The PlayerProfile is set in Init and must not be null.
    public PlayerProfile PlayerProfile { get; private set; }

    [Inject(Optional = true)]
    private SongSelectSceneControl songSelectSceneControl;
    
    [Inject(Optional = true)]
    private PartyModeTeamSettings partyModeTeamSettings;
    
    [Inject(UxmlName = R.UxmlNames.teamLabel)]
    private Label teamLabel;
    
    [Inject(UxmlName = R.UxmlNames.voiceNameLabel)]
    private Label voiceNameLabel;
    
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
                micIcon.style.color = new StyleColor(micProfile.Color);
                micIcon.style.unityBackgroundImageTintColor = new StyleColor(micProfile.Color);
            }

            UpdateMicPitchTracker();
            micProgressBarRecordingControl.MicProfile = micProfile;
            playerProfileImageControl.MicProfile = micProfile;
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

    private MicSelectionDialogControl micSelectionDialogControl;
    private readonly PlayerProfileImageControl playerProfileImageControl = new();
    
    private readonly Subject<MicSelectionDialogControl.MicProfileChangedEvent> micProfileChangedEventStream = new();
    public IObservable<MicSelectionDialogControl.MicProfileChangedEvent> MicProfileChangedEventStream => micProfileChangedEventStream;

    public Action<MicProfile> OnMicProfileSelected { get; set; }
    
    public void OnInjectionFinished()
    {
        InitVoiceSelection();

        InitMicPitchTracker();

        injector.Inject(micProgressBarRecordingControl);
        micProgressBarRecordingControl.MicProfile = MicProfile;

        togglePlayerSelectedButton.RegisterCallbackButtonTriggered(_ => IsSelected.Value = !IsSelected.Value);
        micButton.RegisterCallbackButtonTriggered(_ => OpenMicSelectionDialog());
        
        focusableNavigator.AddCustomNavigationTarget(micButton, Vector2.left, togglePlayerSelectedButton, true);
        
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
            micButton.SetVisibleByDisplay(newValue);

            if (PlayerProfile != null)
            {
                PlayerProfile.IsSelected = newValue;
            }
        });
    }

    private void InitVoiceSelection()
    {
        selectedVoiceName.Subscribe(_ => UpdateToggleVoiceButtonText());
        toggleVoiceButton.RegisterCallbackButtonTriggered(_ =>
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
            voiceNameLabel.text = voiceNames[selectedVoiceName.Value];
        }
        else
        {
            voiceNameLabel.text = selectedVoiceName.Value;
        }
    }

    public void Init(PlayerProfile playerProfile)
    {
        this.PlayerProfile = playerProfile;
        nameLabel.text = playerProfile.Name;
        injector.WithRootVisualElement(playerImage)
            .WithBindingForInstance(playerProfile)
            .Inject(playerProfileImageControl);
        MicProfile = null;

        nameLabel.text = PlayerProfile.Name;
        if (partyModeTeamSettings != null)
        {
            if (songSelectSceneControl.PartyModeSettings.teamSettings.isFreeForAll)
            {
                teamLabel.HideByDisplay();
            }    
            else
            {
                teamLabel.ShowByDisplay();
                teamLabel.text = partyModeTeamSettings.name;
            }
        }
        else
        {
            teamLabel.HideByDisplay();
        }
    }

    private void OpenMicSelectionDialog()
    {
        if (micSelectionDialogControl != null)
        {
            return;
        }

        void OnMicSelected(MicProfile newMicProfile)
        {
            MicProfile = newMicProfile;
            micSelectionDialogControl?.CloseDialog();
            OnMicProfileSelected?.Invoke(newMicProfile);
        }

        VisualElement dialog = messageDialogUi.CloneTreeAndGetFirstChild();
        visualElement.GetRootVisualElement().Add(dialog);

        micSelectionDialogControl = injector
            .WithRootVisualElement(dialog)
            .CreateAndInject<MicSelectionDialogControl>();
        micSelectionDialogControl.Title = $"Select Microphone for {PlayerProfile.Name}";
        micSelectionDialogControl.DialogClosedEventStream.Subscribe(_ => micSelectionDialogControl = null);
        micSelectionDialogControl.OnMicProfileSelected = OnMicSelected;
        micSelectionDialogControl.MicProfiles = micProfiles;
    }
    
    public void SetSelected(bool newValue, bool force)
    {
        if (partyModeTeamSettings != null
            && !force)
        {
            // In party mode, there is always one player selected per team. And these players have been chosen already.
            return;
        }

        IsSelected.Value = newValue;
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
            || micProfile.IsInputFromConnectedClient
            || !settings.SongSelectSettings.micTestActive)
        {
            if (micPitchTracker.MicSampleRecorder.IsRecording.Value)
            {
                micPitchTracker.MicSampleRecorder.StopRecording();
            }
        }
        else if (micProfile != null
                 && !micProfile.IsInputFromConnectedClient
                 && settings.SongSelectSettings.micTestActive)
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
        focusableNavigator.RemoveCustomNavigationTarget(micButton, Vector2.left, true);
    }
}
