using System.Collections.Generic;
using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerSelectPlayerProfileEntryControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    public string PlayerProfileName { get; private set; }
    
    [Inject(UxmlName = R.UxmlNames.micIcon)]
    private VisualElement micIcon;
    
    [Inject(UxmlName = R.UxmlNames.nameLabel)]
    private Label nameLabel;
    
    [Inject(UxmlName = R.UxmlNames.teamLabel)]
    private Label teamLabel;

    [Inject(UxmlName = R.UxmlNames.voiceChooser)]
    private ItemPicker voiceChooser;

    [Inject(UxmlName = R.UxmlNames.enabledToggle)]
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

    public void OnInjectionFinished()
    {
        nameLabel.text = PlayerProfileName;
        teamLabel.HideByDisplay();

        VoiceChooserControl = new(voiceChooser, new List<string>()
        {
            Voice.firstVoiceName,
            Voice.secondVoiceName,
        });

        UpdateMicIcon();
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
}
