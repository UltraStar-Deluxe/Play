using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

public static class PlayerProfilesManager
{
    private static List<PlayerProfile> s_playerProfiles;

    public static List<PlayerProfile> PlayerProfiles
    {
        get {
            if(s_playerProfiles == null) {
                InitPlayerProfiles();
            }
            return s_playerProfiles; 
        }
    }

    private static void InitPlayerProfiles()
    {
        var xconfig = XElement.Load("./Config.xml");
        var xplayerProfiles = xconfig.Element("Profiles");
        s_playerProfiles = xplayerProfiles.Elements("PlayerProfile")
            .Select(xplayerProfile => {
                var playerProfile = new PlayerProfile();
                playerProfile.Name = xplayerProfile.Attribute("name").Value;
                playerProfile.MicDevice = xplayerProfile.Attribute("mic").Value;
                return playerProfile;
                } ).ToList();
    }
}
