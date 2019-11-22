using UnityEngine;
using UnityEngine.UI;

public class PlayerNameText : MonoBehaviour
{
    private Text text;

    void OnEnable()
    {
        text = GetComponent<Text>();
    }

    public void SetText(string value)
    {
        text.text = value;
    }

    public void SetPlayerProfile(PlayerProfile playerProfile)
    {
        text.text = playerProfile.Name;
    }

    public void SetColorOfMicProfile(MicProfile micProfile)
    {
        text.color = micProfile.Color;
    }
}
