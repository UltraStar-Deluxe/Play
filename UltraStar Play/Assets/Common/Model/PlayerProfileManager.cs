using System.Collections.Generic;
using UnityEngine;

public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<PlayerProfileManager>("PlayerProfileManager");
        }
    }

    private static List<PlayerProfile> playerProfiles = new List<PlayerProfile>();
    public List<PlayerProfile> PlayerProfiles
    {
        get
        {
            if (playerProfiles.Count == 0)
            {
                LoadPlayerProfiles();
            }
            return playerProfiles;
        }
    }

    private void LoadPlayerProfiles()
    {
        playerProfiles = new List<PlayerProfile>(SettingsManager.Instance.Settings.PlayerProfiles);
    }
}
