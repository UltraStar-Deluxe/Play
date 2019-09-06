using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

public class PlayerProfileManager : MonoBehaviour
{
    public static PlayerProfileManager Instance
    {
        get
        {
            GameObject obj = GameObject.FindGameObjectWithTag("PlayerProfileManager");
            if (obj)
            {
                return obj.GetComponent<PlayerProfileManager>();
            }
            else
            {
                return null;
            }
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
        playerProfiles = new List<PlayerProfile>();

        XElement xconfig = XElement.Load("./Config.xml");
        XElement xprofiles = xconfig.Element("Profiles");
        IEnumerable<XElement> xplayerProfiles = xprofiles.Elements("PlayerProfile");
        foreach (XElement xplayerProfile in xplayerProfiles)
        {
            PlayerProfile playerProfile = CreatePlayerProfile(xplayerProfile);
            playerProfiles.Add(playerProfile);
        }
    }

    private PlayerProfile CreatePlayerProfile(XElement xplayerProfile)
    {
        PlayerProfile playerProfile = new PlayerProfile();
        playerProfile.Name = xplayerProfile.Attribute("name").Value;
        playerProfile.MicDevice = xplayerProfile.Attribute("mic").Value;
        return playerProfile;
    }
}
