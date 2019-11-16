using UnityEngine;
using UnityEngine.UI;

public class SongSelectPlayerProfileListEntry : MonoBehaviour
{
    public Image micImage;
    public Text nameLabel;
    public Toggle isSelectedToggle;

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
                micImage.enabled = false;
            }
            else
            {
                micImage.enabled = true;
                micImage.color = micProfile.Color;
            }
        }
    }

    public bool IsSelected
    {
        get
        {
            return isSelectedToggle.isOn;
        }
    }

    public void Init(PlayerProfile playerProfile)
    {
        this.PlayerProfile = playerProfile;
        nameLabel.text = playerProfile.Name;
        MicProfile = null;
    }
}