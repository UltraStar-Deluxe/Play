using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SongSelectPlayerEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement visualElement;

    [Inject(Key = nameof(micProfiles))]
    private List<MicProfile> micProfiles;
        
    [Inject(Key = nameof(messageDialogUi))]
    private VisualTreeAsset messageDialogUi;
    
    [Inject(UxmlName = R.UxmlNames.dialogContainer)]
    private VisualElement dialogContainer;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.micButton)]
    private Button micButton;
    
    [Inject(UxmlName = R_PlayShared.UxmlNames.micIcon)]
    private VisualElement micIcon;

    [Inject(UxmlName = R_PlayShared.UxmlNames.nameLabel)]
    private Label nameLabel;

    [Inject(UxmlName = R_PlayShared.UxmlNames.teamLabel)]
    private Label teamLabel;

    [Inject(UxmlName = R_PlayShared.UxmlNames.enabledToggle)]
    private Toggle enabledToggle;

    [Inject]
    public PlayerProfile PlayerProfile { get; private set; }

    [Inject]
    private Injector injector;

    [Inject(Optional = true)]
    private PartyModeTeamSettings partyModeTeamSettings;

    [Inject(Optional = true)]
    private SongSelectSceneControl songSelectSceneControl;

    private LabeledItemPickerControl<Voice> voiceChooserControl;

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
        }
    }

    public Voice Voice => voiceChooserControl.ItemPicker.IsVisibleByDisplay()
        ? voiceChooserControl.Selection.Value
        : null;

    public bool IsSelected
    {
        get
        {
            return PlayerProfile.IsSelected;
        }
    }

    private MicSelectionDialogControl micSelectionDialogControl;
    
    private readonly Subject<bool> selectedChangedEventStream = new();
    public IObservable<bool> SelectedChangedEventStream => selectedChangedEventStream;

    private readonly Subject<MicSelectionDialogControl.MicProfileChangedEvent> micProfileChangedEventStream = new();
    public IObservable<MicSelectionDialogControl.MicProfileChangedEvent> MicProfileChangedEventStream => micProfileChangedEventStream;

    public Action<MicProfile> OnMicProfileSelected { get; set; }
    
    public void OnInjectionFinished()
    {
        voiceChooserControl = new LabeledItemPickerControl<Voice>(visualElement.Q<ItemPicker>(R_PlayShared.UxmlNames.voiceChooser), new List<Voice>());
        voiceChooserControl.GetLabelTextFunction = voice => voice != null
            ? voice.Name
            : "";

        micButton.RegisterCallbackButtonTriggered(() => OpenMicSelectionDialog());
        
        UpdateEnabledToggle();
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

            enabledToggle.HideByDisplay();
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
            micSelectionDialogControl.CloseDialog();
            OnMicProfileSelected?.Invoke(newMicProfile);
        }

        VisualElement dialog = messageDialogUi.CloneTreeAndGetFirstChild();
        dialogContainer.Add(dialog);
        dialogContainer.ShowByDisplay();
        
        micSelectionDialogControl = injector
            .WithRootVisualElement(dialog)
            .CreateAndInject<MicSelectionDialogControl>();
        micSelectionDialogControl.Title = $"Select Microphone for {PlayerProfile.Name}";
        micSelectionDialogControl.DialogClosedEventStream.Subscribe(_ => OnMicSelectionDialogClosed());
        micSelectionDialogControl.OnMicProfileSelected = OnMicSelected;
        micSelectionDialogControl.MicProfiles = micProfiles;
    }

    private void OnMicSelectionDialogClosed()
    {
        if (micSelectionDialogControl == null)
        {
            return;
        }
        micSelectionDialogControl = null;
        dialogContainer.HideByDisplay();
    }
    
    public void SetSelected(bool newValue, bool force)
    {
        if (partyModeTeamSettings != null
            && !force)
        {
            // In party mode, there is always one player selected per team. And these players have been chosen already.
            return;
        }

        Debug.Log($"Select player profile '{PlayerProfile.Name}': {newValue}");
        PlayerProfile.IsSelected = newValue;
        UpdateEnabledToggle();
        selectedChangedEventStream.OnNext(newValue);
    }

    private void UpdateEnabledToggle()
    {
        enabledToggle.value = PlayerProfile.IsSelected;
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
}
