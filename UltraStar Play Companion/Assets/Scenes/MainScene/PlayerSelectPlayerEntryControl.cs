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

    [Inject(UxmlName = R.UxmlNames.dialogContainer)]
    private VisualElement dialogContainer;

    [Inject(UxmlName = R_PlayShared.UxmlNames.micButton)]
    private Button micButton;

    [Inject(UxmlName = R_PlayShared.UxmlNames.micIcon)]
    private VisualElement micIcon;

    [Inject(UxmlName = R.UxmlNames.noMicIcon)]
    private VisualElement noMicIcon;

    [Inject(UxmlName = R_PlayShared.UxmlNames.nameLabel)]
    private Label nameLabel;

    [Inject(UxmlName = R.UxmlNames.teamLabel)]
    private Label teamLabel;

    [Inject(UxmlName = R.UxmlNames.voiceChooser)]
    private Chooser voiceChooser;

    [Inject(UxmlName = R.UxmlNames.selectedToggle)]
    private Toggle selectedToggle;

    [Inject(UxmlName = R.UxmlNames.horizontalSeparatorLine)]
    private VisualElement horizontalSeparatorLine;

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

    public ReactiveProperty<bool> IsSelected { get; private set; } = new();

    public LabeledChooserControl<EExtendedVoiceId> VoiceChooserControl { get; private set; }

    private MicSelectionDialogControl micSelectionDialogControl;

    public Action<MicProfile> OnMicProfileSelected { get; set; }

    public void OnInjectionFinished()
    {
        nameLabel.text = PlayerProfileName;
        teamLabel.HideByDisplay();

        nameLabel.RegisterCallback<PointerDownEvent>(_ => ToggleSelected());
        selectedToggle.RegisterValueChangedCallback(evt => IsSelected.Value = evt.newValue);
        micButton.RegisterCallbackButtonTriggered(_ => OpenMicSelectionDialog());
        IsSelected.Subscribe(newValue =>
        {
            selectedToggle.value = newValue;
            UpdateMicIcon();
        });

        VoiceChooserControl = new EnumChooserControl<EExtendedVoiceId>(voiceChooser);

        UpdateMicIcon();
    }

    private void ToggleSelected()
    {
        IsSelected.Value = !IsSelected.Value;
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
            micSelectionDialogControl.CloseDialog();
        }

        VisualElement dialog = messageDialogUi.CloneTreeAndGetFirstChild();
        dialogContainer.Add(dialog);
        dialogContainer.ShowByDisplay();

        micSelectionDialogControl = injector
            .WithRootVisualElement(dialog)
            .CreateAndInject<MicSelectionDialogControl>();
        micSelectionDialogControl.Title = Translation.Of($"Select Microphone for {PlayerProfileName}");
        micSelectionDialogControl.AddButton(Translation.Get(R.Messages.common_ok), _ => micSelectionDialogControl.CloseDialog());
        micSelectionDialogControl.ShowInfoLabel = false;
        micSelectionDialogControl.DialogClosedEventStream.Subscribe(_ => OnMicSelectionDialogClosed());
        micSelectionDialogControl.MicProfiles = micProfiles;
        micSelectionDialogControl.OnMicProfileSelected = OnMicSelected;
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
            micIcon.style.color = new StyleColor(micProfile.Color);
            micIcon.ShowByVisibility();
            noMicIcon.HideByDisplay();
        }
        else
        {
            micIcon.HideByVisibility();
            noMicIcon.SetVisibleByDisplay(IsSelected.Value);
        }
    }

    public void SetAvailableVoiceIds(List<EExtendedVoiceId> voiceIds)
    {
        VoiceChooserControl.Items = voiceIds;
        if (VoiceChooserControl.Items.Count <= 1)
        {
            VoiceChooserControl.Chooser.HideByDisplay();
        }
    }

    public void SetSeparatorVisibleByDisplay(bool newValue)
    {
        horizontalSeparatorLine.SetVisibleByDisplay(newValue);
    }
}
