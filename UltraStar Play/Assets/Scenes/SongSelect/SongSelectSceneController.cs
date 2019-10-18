using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Linq;
using System;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;

public class SongSelectSceneController : MonoBehaviour
{
    public RectTransform songListContent;
    public RectTransform songButtonPrefab;

    public RectTransform playerProfileListContent;
    public RectTransform playerProfileButtonPrefab;

    private PlayerProfile selectedPlayerProfile;

    void Start()
    {
        List<SongMeta> songMetas = SongMetaManager.Instance.SongMetas;
        PopulateSongList(songMetas);
        List<PlayerProfile> playerProfiles = PlayerProfileManager.Instance.PlayerProfiles;
        PopulatePlayerProfileList(playerProfiles);
    }

    private void PopulatePlayerProfileList(List<PlayerProfile> playerProfiles)
    {
        // Remove old buttons.
        foreach (RectTransform element in playerProfileListContent)
        {
            GameObject.Destroy(element.gameObject);
        }

        // Create new buttons. One for each profile.
        foreach (PlayerProfile playerProfile in playerProfiles)
        {
            AddPlayerProfileButton(playerProfile);
        }
    }

    private void AddPlayerProfileButton(PlayerProfile playerProfile)
    {
        RectTransform newButton = RectTransform.Instantiate(playerProfileButtonPrefab);
        newButton.SetParent(playerProfileListContent);

        newButton.GetComponentInChildren<Text>().text = playerProfile.Name;
        newButton.GetComponent<Button>().onClick.AddListener(() => OnPlayerProfileButtonClicked(playerProfile));
    }

    private void PopulateSongList(List<SongMeta> songMetas)
    {
        // Remove old buttons.
        foreach (RectTransform element in songListContent)
        {
            GameObject.Destroy(element.gameObject);
        }

        // Create new song buttons. One for each loaded song.
        foreach (SongMeta songMeta in songMetas)
        {
            AddSongButton(songMeta);
        }
    }

    private void AddSongButton(SongMeta songMeta)
    {
        RectTransform newButton = RectTransform.Instantiate(songButtonPrefab);
        newButton.SetParent(songListContent);

        newButton.GetComponentInChildren<Text>().text = songMeta.Title;
        newButton.GetComponent<Button>().onClick.AddListener(() => OnSongButtonClicked(songMeta));
    }

    private void OnSongButtonClicked(SongMeta songMeta)
    {
        SingSceneData singSceneData = new SingSceneData();
        singSceneData.SelectedSongMeta = songMeta;

        List<PlayerProfile> allPlayerProfiles = PlayerProfileManager.Instance.PlayerProfiles;
        PlayerProfile defaultPlayerProfile = allPlayerProfiles[0];
        PlayerProfile playerProfile = selectedPlayerProfile.OrIfNull(defaultPlayerProfile);
        singSceneData.AddPlayerProfile(playerProfile);

        SceneNavigator.Instance.LoadScene(EScene.SingScene, singSceneData);
    }

    private void OnPlayerProfileButtonClicked(PlayerProfile playerProfile)
    {
        Debug.Log($"Clicked on player profile button: {playerProfile.Name}");
        selectedPlayerProfile = playerProfile;
    }

}
