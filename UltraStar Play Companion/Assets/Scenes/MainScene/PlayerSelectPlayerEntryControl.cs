using System;
using System.Collections.Generic;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerSelectPlayerEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    public string PlayerProfileName { get; private set; }
    
    [Inject]
    private Injector injector;
    
    [Inject(Key = nameof(micProfiles))]
    private List<MicProfile> micProfiles;
        
    [Inject(Key = nameof(messageDialogUi))]
    private VisualTreeAsset messageDialogUi;
    
    [Inject(Key = nameof(micWithNameUi))]
    private VisualTreeAsset micWithNameUi;
    
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

    [Inject(UxmlName = R_PlayShared.UxmlNames.voiceChooser)]
    private ItemPicker voiceChooser;

    [Inject(UxmlName = R_PlayShared.UxmlNames.enabledToggle)]
    public Toggle EnabledToggle { get; private set; }
    
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
            UpdateMicIcon();
        }
    }

    public bool IsSelected => EnabledToggle.value;
    
    public LabeledItemPickerControl<string> VoiceChooserControl { get; private set; }

    private MessageDialogControl micSelectionDialogControl;
    
    private readonly Subject<MicProfileChangedEvent> micProfileChangedEventStream = new();
    public IObservable<MicProfileChangedEvent> MicProfileChangedEventStream => micProfileChangedEventStream;

    public void OnInjectionFinished()
    {
        nameLabel.text = PlayerProfileName;
        teamLabel.HideByDisplay();

        micButton.RegisterCallbackButtonTriggered(() => OpenMicSelectionDialog());
        
        VoiceChooserControl = new(voiceChooser, new List<string>()
        {
            Voice.firstVoiceName,
            Voice.secondVoiceName,
        });

        UpdateMicIcon();
    }

    private void OpenMicSelectionDialog()
    {
        if (micSelectionDialogControl != null)
        {
            return;
        }

        void OnMicSelected(MicProfile newMicProfile)
        {
            micProfileChangedEventStream.OnNext(new()
            {
                oldMicProfile = micProfile,
                newMicProfile = newMicProfile,
                playerEntryControl = this,
            });
            MicProfile = newMicProfile;
            micSelectionDialogControl.CloseDialog();
        }

        VisualElement dialog = messageDialogUi.CloneTreeAndGetFirstChild();
        dialogContainer.Add(dialog);
        dialogContainer.ShowByDisplay();
        
        micSelectionDialogControl = injector
            .WithRootVisualElement(dialog)
            .CreateAndInject<MessageDialogControl>();
        micSelectionDialogControl.Title = $"Select Microphone for {PlayerProfileName}";
        micSelectionDialogControl.AddButton("OK", () => micSelectionDialogControl.CloseDialog());
        micSelectionDialogControl.DialogClosedEventStream.Subscribe(_ => OnMicSelectionDialogClosed());
        
        micProfiles.ForEach(otherMicProfile =>
        {
            VisualElement micWithName = micWithNameUi.CloneTreeAndGetFirstChild();
            micWithName.Q<Button>(R_PlayShared.UxmlNames.micButton).RegisterCallbackButtonTriggered(() => OnMicSelected(otherMicProfile));
            micWithName.Q<Label>(R_PlayShared.UxmlNames.nameLabel).text = otherMicProfile.Name;
            micWithName.Q<Label>(R_PlayShared.UxmlNames.nameLabel).RegisterCallback<ClickEvent>(evt => OnMicSelected(otherMicProfile));
            micWithName.Q<VisualElement>(R_PlayShared.UxmlNames.micIcon).style.unityBackgroundImageTintColor = new StyleColor(otherMicProfile.Color);
            
            micSelectionDialogControl.AddVisualElement(micWithName);
        });
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
    
    private void UpdateMicIcon()
    {
        if (micProfile != null)
        {
            micIcon.style.unityBackgroundImageTintColor = new StyleColor(micProfile.Color);
            micIcon.ShowByVisibility();
        }
        else
        {
            micIcon.HideByVisibility();
        }
    }

    public void SetAvailableVoiceNames(List<string> voiceNames)
    {
        VoiceChooserControl.Items = voiceNames;
        if (VoiceChooserControl.Items.Count <= 1)
        {
            VoiceChooserControl.ItemPicker.HideByDisplay();
        }
    }

    public class MicProfileChangedEvent
    {
        public PlayerSelectPlayerEntryControl playerEntryControl;
        public MicProfile oldMicProfile;
        public MicProfile newMicProfile;
    }
}
