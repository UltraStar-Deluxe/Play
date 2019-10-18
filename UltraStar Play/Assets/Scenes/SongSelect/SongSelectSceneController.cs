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
    public ArtistText artistText;
    public Text songTitleText;
    public Text songCountText;
    public GameObject videoIndicator;
    public GameObject duetIndicator;

    public RectTransform playerProfileListContent;
    public RectTransform playerProfileButtonPrefab;

    private PlayerProfile selectedPlayerProfile;

    private SongRouletteController songRouletteController;

    private SongSelectSceneData sceneData;

    public static SongSelectSceneController Instance
    {
        get
        {
            return FindObjectOfType<SongSelectSceneController>();
        }
    }

    void Start()
    {
        sceneData = SceneNavigator.Instance.GetSceneData(CreateDefaultSceneData());

        List<SongMeta> songMetas = SongMetaManager.Instance.SongMetas;
        List<PlayerProfile> playerProfiles = PlayerProfileManager.Instance.PlayerProfiles;
        PopulatePlayerProfileList(playerProfiles);

        songRouletteController = FindObjectOfType<SongRouletteController>();
        songRouletteController.SongSelectSceneController = this;
        songRouletteController.SetSongs(songMetas);
        if (sceneData.SongMeta != null)
        {
            songRouletteController.SelectSong(sceneData.SongMeta);
        }
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

    private void StartSingScene(SongMeta songMeta)
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

    private SongSelectSceneData CreateDefaultSceneData()
    {
        SongSelectSceneData sceneData = new SongSelectSceneData();
        return sceneData;
    }

    public void OnSongSelected(SongMeta selectedSong, int selectedSongIndex, List<SongMeta> songs)
    {
        artistText.SetText(selectedSong.Artist);
        songTitleText.text = selectedSong.Title;
        songCountText.text = (selectedSongIndex + 1) + "/" + songs.Count;

        bool hasVideo = !string.IsNullOrEmpty(selectedSong.Video);
        videoIndicator.SetActive(hasVideo);

        bool isDuet = selectedSong.VoiceNames.Keys.Count == 2;
        duetIndicator.SetActive(isDuet);
    }

    public void OnNextSong()
    {
        songRouletteController.SelectNextSong();
    }

    public void OnPreviousSong()
    {
        songRouletteController.SelectPreviousSong();
    }

    public void OnStartSingScene()
    {
        StartSingScene(songRouletteController.SelectedSong);
    }
}
