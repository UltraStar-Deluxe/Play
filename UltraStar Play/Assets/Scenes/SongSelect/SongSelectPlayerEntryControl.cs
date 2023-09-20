using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SongSelectPlayerEntryControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        micSelectionDialogControl = null;
    }
    private static MicSelectionDialogControl micSelectionDialogControl;
    public static MicSelectionDialogControl MicSelectionDialogControl => micSelectionDialogControl;

    [Inject(Key = nameof(micPitchTrackerPrefab))]
    private NewestSamplesMicPitchTracker micPitchTrackerPrefab;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;

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

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private ListViewH songListView;

    [Inject(UxmlName = R.UxmlNames.changeVoiceButton)]
    private Button changeVoiceButton;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager;

    [Inject]
    private NonPersistentSettings nonPersistentSettings;

    [Inject]
    private FocusableNavigator focusableNavigator;

    [Inject]
    private SongSelectPlayerListControl selectPlayerListControl;

    [Inject]
    private MicSampleRecorderManager micSampleRecorderManager;

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

    private readonly ReactiveProperty<string> selectedVoiceName = new(Voice.firstVoiceId);
    public string VoiceName => changeVoiceButton.IsVisibleByDisplay()
        ? selectedVoiceName.Value
        : null;

    private NewestSamplesMicPitchTracker micPitchTracker;

    private readonly MicProgressBarRecordingControl micProgressBarRecordingControl = new();

    public ReactiveProperty<bool> IsSelected {get; private set; } = new(false);

    private Dictionary<string, string> voiceIdToDisplayName;

    private readonly PlayerProfileImageControl playerProfileImageControl = new();

    private readonly Subject<MicSelectionDialogControl.MicProfileChangedEvent> micProfileChangedEventStream = new();
    public IObservable<MicSelectionDialogControl.MicProfileChangedEvent> MicProfileChangedEventStream => micProfileChangedEventStream;

    public Action<MicProfile> OnMicProfileSelected { get; set; }

    private int lastUpdateAllMicPitchTrackersFrameCount;

    public void OnInjectionFinished()
    {
        InitVoiceSelection();

        InitMicPitchTracker();

        injector.Inject(micProgressBarRecordingControl);
        micProgressBarRecordingControl.MicProfile = MicProfile;

        togglePlayerSelectedButton.RegisterCallbackButtonTriggered(_ => IsSelected.Value = !IsSelected.Value);
        micButton.RegisterCallbackButtonTriggered(_ => OpenMicSelectionDialog());

        focusableNavigator.AddCustomNavigationTarget(micButton, Vector2.left, togglePlayerSelectedButton, true);
        focusableNavigator.AddCustomNavigationTarget(micButton, Vector2.up, songListView);
        focusableNavigator.AddCustomNavigationTarget(togglePlayerSelectedButton, Vector2.up, songListView);
        focusableNavigator.AddCustomNavigationTarget(togglePlayerSelectedButton, Vector2.down, changeVoiceButton, true);

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

        nonPersistentSettings.MicTestActive.Subscribe(_ => UpdateAllMicPitchTrackers());
    }

    private void InitVoiceSelection()
    {
        selectedVoiceName.Subscribe(_ => UpdateChangeVoiceButtonText());
        changeVoiceButton.RegisterCallbackButtonTriggered(_ =>
        {
            selectedVoiceName.Value = Voice.GetNextVoiceId(selectedVoiceName.Value);
        });
    }

    private void UpdateChangeVoiceButtonText()
    {
        if (!voiceIdToDisplayName.IsNullOrEmpty()
            && voiceIdToDisplayName.ContainsKey(selectedVoiceName.Value))
        {
            voiceNameLabel.text = voiceIdToDisplayName[selectedVoiceName.Value];
        }
        else if (selectedVoiceName.Value == Voice.mergedVoiceId)
        {
            voiceNameLabel.text = "Both";
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
            if (songSelectSceneControl.PartyModeSettings.TeamSettings.IsFreeForAll)
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

    public void Update()
    {
        micProgressBarRecordingControl.Update();
        micSelectionDialogControl?.Update();
    }

    private void OpenMicSelectionDialog()
    {
        if (micSelectionDialogControl != null)
        {
            return;
        }

        void OnMicSelected(MicProfile newMicProfile)
        {
            OnMicProfileSelected?.Invoke(newMicProfile);
            MicProfile = newMicProfile;
            micSelectionDialogControl?.CloseDialog();
            UpdateMicPitchTracker();
        }

        VisualElement dialog = messageDialogUi.CloneTreeAndGetFirstChild();
        visualElement.GetRootVisualElement().Add(dialog);

        micSelectionDialogControl = injector
            .WithRootVisualElement(dialog)
            .CreateAndInject<MicSelectionDialogControl>();
        micSelectionDialogControl.Title = $"Select Microphone for {PlayerProfile.Name}";
        micSelectionDialogControl.DialogClosedEventStream.Subscribe(_ =>
        {
            if (micSelectionDialogControl == null)
            {
                return;
            }
            micSelectionDialogControl = null;

            StopRecordingWithAllMicrophones();

            UpdateAllMicPitchTrackers();
        });
        micSelectionDialogControl.OnMicProfileSelected = OnMicSelected;
        micSelectionDialogControl.MicProfiles = GetAvailableMicProfiles();

        // Start recording to select microphone
        StartRecordingWithAllMicrophones();
    }

    private void StartRecordingWithAllMicrophones()
    {
        foreach (MicProfile availableMicProfile in GetAvailableMicProfiles())
        {
            MicSampleRecorder micSampleRecorder = micSampleRecorderManager.GetOrCreateMicSampleRecorder(availableMicProfile);
            if (micSampleRecorder != null
                && !micSampleRecorder.IsRecording.Value)
            {
                micSampleRecorder.StartRecording();
            }
        }

        SendStartRecordingMessageToAllConnectedClients();
    }

    private void StopRecordingWithAllMicrophones()
    {
        Debug.Log($"StopRecordingWithAllMicrophones: frame: {Time.frameCount}");

        foreach (MicProfile availableMicProfile in GetAvailableMicProfiles())
        {
            MicSampleRecorder micSampleRecorder = micSampleRecorderManager.GetOrCreateMicSampleRecorder(availableMicProfile);
            if (micSampleRecorder != null
                && micSampleRecorder.IsRecording.Value)
            {
                micSampleRecorder.StopRecording();
            }
        }

        SendStopRecordingMessageToAllConnectedClients();
    }

    private List<MicProfile> GetAvailableMicProfiles()
    {
        return SettingsUtils.GetAvailableMicProfiles(settings, themeManager, serverSideConnectRequestManager);
    }

    private List<ConnectedClientHandlerAndMicProfile> GetConnectedClientHandlers()
    {
        return serverSideConnectRequestManager.GetConnectedClientHandlers(GetAvailableMicProfiles());
    }

    private void SendStopRecordingMessageToAllConnectedClients()
    {
        GetConnectedClientHandlers().ForEach(it => it.ConnectedClientHandler.SendMessageToClient(new StopRecordingMessageDto()));
    }

    private void SendStartRecordingMessageToAllConnectedClients()
    {
        GetConnectedClientHandlers().ForEach(it => it.ConnectedClientHandler.SendMessageToClient(new StartRecordingMessageDto()));
    }

    private ConnectedClientHandlerAndMicProfile GetConnectedClientHandler()
    {
        if (micProfile == null)
        {
            return null;
        }
        return serverSideConnectRequestManager.GetConnectedClientHandlers(new List<MicProfile> { micProfile }).FirstOrDefault();
    }

    private void SendStopRecordingMessageToConnectedClient()
    {
        if (micProfile != null
            && micProfile.IsInputFromConnectedClient)
        {
            GetConnectedClientHandler()?.ConnectedClientHandler.SendMessageToClient(new StopRecordingMessageDto());
        }
    }

    private void SendStartRecordingMessageToConnectedClient()
    {
        if (micProfile != null
            && micProfile.IsInputFromConnectedClient)
        {
            GetConnectedClientHandler()?.ConnectedClientHandler.SendMessageToClient(new StartRecordingMessageDto());
        }
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
        changeVoiceButton.HideByDisplay();
    }

    public void ShowVoiceSelection(SongMeta songMeta, int selectedVoiceIndex)
    {
        voiceIdToDisplayName = SongMetaUtils.GetVoiceIdToDisplayName(songMeta);
        changeVoiceButton.ShowByDisplay();
        UpdateChangeVoiceButtonText();
        selectedVoiceName.Value = selectedVoiceIndex == 0
            ? Voice.firstVoiceId
            : Voice.secondVoiceId;
    }

    private void InitMicPitchTracker()
    {
        micPitchTracker = GameObject.Instantiate(micPitchTrackerPrefab);
        injector.InjectAllComponentsInChildren(micPitchTracker);
        micPitchTracker.MicProfile = micProfile;
        UpdateMicPitchTracker();
    }

    private void UpdateAllMicPitchTrackers()
    {
        if (lastUpdateAllMicPitchTrackersFrameCount == Time.frameCount)
        {
            return;
        }

        songSelectSceneControl.playerListControl.PlayerEntryControls
            .ForEach(it => it.UpdateMicPitchTracker());

        lastUpdateAllMicPitchTrackersFrameCount = Time.frameCount;
    }

    public void UpdateMicPitchTracker()
    {
        if (micPitchTracker == null)
        {
            return;
        }

        micPitchTracker.MicProfile = micProfile;
        if (micProfile == null)
        {
            return;
        }

        if (nonPersistentSettings.MicTestActive.Value
            || micSelectionDialogControl != null)
        {
            if (micProfile.IsInputFromConnectedClient)
            {
                SendStartRecordingMessageToConnectedClient();
            }
            else if (!micPitchTracker.IsRecording.Value)
            {
                micPitchTracker.StartRecording();
            }
        }
        else
        {
            if (micProfile.IsInputFromConnectedClient)
            {
                SendStopRecordingMessageToConnectedClient();
            }
            else if (micPitchTracker.IsRecording.Value)
            {
                micPitchTracker.StopRecording();
            }
        }
    }

    public void Dispose()
    {
        GameObject.Destroy(micPitchTracker);
        micProgressBarRecordingControl.Dispose();
        focusableNavigator.RemoveCustomNavigationTarget(micButton, Vector2.left, true);
    }
}
