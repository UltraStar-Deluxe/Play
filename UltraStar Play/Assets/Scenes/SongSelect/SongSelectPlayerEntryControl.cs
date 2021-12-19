using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
using Toggle = UnityEngine.UIElements.Toggle;

public class SongSelectPlayerEntryControl : INeedInjection
{
    private readonly VisualElement visualElement;

    [Inject(UxmlName = R.UxmlNames.micIcon)]
    private VisualElement micIcon;

    [Inject(UxmlName = R.UxmlNames.nameLabel)]
    private Label nameLabel;

    [Inject(UxmlName = R.UxmlNames.enabledToggle)]
    public Toggle EnabledToggle { get; private set; }

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
        }
    }

    public bool IsSelected
    {
        get
        {
            return EnabledToggle.value;
        }
    }

    public SongSelectPlayerEntryControl(VisualElement visualElement)
    {
        this.visualElement = visualElement;
        voiceChooserControl = new LabeledItemPickerControl<Voice>(visualElement.Q<ItemPicker>(R.UxmlNames.voiceChooser), new List<Voice>());
        voiceChooserControl.GetLabelTextFunction = voice => voice != null
            ? voice.Name
            : "";
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
}
