using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

public class PlayerProfilesManager : MonoBehaviour
{
    void OnEnable()
    {
        if (!SceneDataBus.HasData(ESceneData.AllPlayerProfiles))
        {
            LoadPlayerProfiles();
        }
    }

    private void LoadPlayerProfiles()
    {
        XElement xconfig = XElement.Load("./Config.xml");
        XElement xplayerProfiles = xconfig.Element("Profiles");
        List<PlayerProfile> playerProfiles = xplayerProfiles.Elements("PlayerProfile")
            .Select(xplayerProfile =>
            {
                var playerProfile = new PlayerProfile();
                playerProfile.Name = xplayerProfile.Attribute("name").Value;
                playerProfile.MicDevice = xplayerProfile.Attribute("mic").Value;
                return playerProfile;
            }).ToList();

        SceneDataBus.PutData(ESceneData.AllPlayerProfiles, playerProfiles);
    }
}
