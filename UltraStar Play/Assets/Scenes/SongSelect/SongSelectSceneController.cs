using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Xml.Linq;
using System;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;
using UniRx;

public class SongSelectSceneController : MonoBehaviour
{
    public static SongSelectSceneController Instance
    {
        get
        {
            return FindObjectOfType<SongSelectSceneController>();
        }
    }

    public ArtistText artistText;
    public Text songTitleText;
    public Text songCountText;
    public GameObject videoIndicator;
    public GameObject duetIndicator;

    public RectTransform playerProfileListContent;
    public RectTransform playerProfileButtonPrefab;

    private SearchInputField searchTextInputField;

    private PlayerProfile selectedPlayerProfile;

    private SongRouletteController songRouletteController;

    private SongSelectSceneData sceneData;
    private List<SongMeta> songMetas;

    private SongMeta selectedSongBeforeSearch;

    private SongMeta selectedSong
    {
        get
        {
            return songRouletteController.Selection.Value.SongMeta;
        }
    }

    void Start()
    {
        sceneData = SceneNavigator.Instance.GetSceneData(CreateDefaultSceneData());

        searchTextInputField = GameObjectUtils.FindObjectOfType<SearchInputField>(true);

        songMetas = SongMetaManager.Instance.SongMetas;
        List<PlayerProfile> playerProfiles = PlayerProfileManager.Instance.PlayerProfiles;
        PopulatePlayerProfileList(playerProfiles);

        songRouletteController = FindObjectOfType<SongRouletteController>();
        songRouletteController.SongSelectSceneController = this;
        songRouletteController.Selection.Subscribe(OnNewSongSelection);

        songRouletteController.SetSongs(songMetas);
        if (sceneData.SongMeta != null)
        {
            songRouletteController.SelectSong(sceneData.SongMeta);
        }
    }

    private void OnNewSongSelection(SongSelection selection)
    {
        SongMeta selectedSong = selection.SongMeta;
        if (selectedSong == null)
        {
            SetEmptySongDetails();
            return;
        }

        artistText.SetText(selectedSong.Artist);
        songTitleText.text = selectedSong.Title;
        songCountText.text = (selection.SongIndex + 1) + "/" + selection.SongsCount;

        bool hasVideo = !string.IsNullOrEmpty(selectedSong.Video);
        videoIndicator.SetActive(hasVideo);

        bool isDuet = selectedSong.VoiceNames.Keys.Count > 1;
        duetIndicator.SetActive(isDuet);
    }

    private void PopulatePlayerProfileList(List<PlayerProfile> playerProfiles)
    {
        // Remove old buttons.
        foreach (RectTransform element in playerProfileListContent)
        {
            Destroy(element.gameObject);
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
        selectedPlayerProfile = playerProfile;
    }

    private SongSelectSceneData CreateDefaultSceneData()
    {
        SongSelectSceneData sceneData = new SongSelectSceneData();
        return sceneData;
    }

    private void SetEmptySongDetails()
    {
        artistText.SetText("");
        // songTitleText.text = "";
        songCountText.text = "0/0";
        videoIndicator.SetActive(false);
        duetIndicator.SetActive(false);
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
        if (selectedSong != null)
        {
            StartSingScene(selectedSong);
        }
    }

    public void OnSearchTextChanged()
    {
        SongMeta lastSelectedSong = selectedSong;
        string searchText = searchTextInputField.Text.ToLower();
        if (string.IsNullOrEmpty(searchText))
        {
            songRouletteController.SetSongs(songMetas);
            if (lastSelectedSong != null)
            {
                songRouletteController.SelectSong(lastSelectedSong);
            }
            else if (selectedSongBeforeSearch != null)
            {
                songRouletteController.SelectSong(selectedSongBeforeSearch);
            }
        }
        else
        {
            switch (searchTextInputField.SearchMode)
            {
                case SearchInputField.ESearchMode.BySongTitle:
                    DoSearch((songMeta) => songMeta.Title.ToLower().Contains(searchText));
                    break;
                case SearchInputField.ESearchMode.ByArtist:
                    DoSearch((songMeta) => songMeta.Artist.ToLower().Contains(searchText));
                    break;
            }
        }
    }

    private void DoSearch(Func<SongMeta, bool> condition)
    {
        List<SongMeta> matchingSongs = songMetas.Where(condition).ToList();
        songRouletteController.SetSongs(matchingSongs);
    }

    public void EnableSearch(SearchInputField.ESearchMode searchMode)
    {
        selectedSongBeforeSearch = selectedSong;

        searchTextInputField.Show();
        searchTextInputField.RequestFocus();
        searchTextInputField.SearchMode = searchMode;
        searchTextInputField.Text = "";
    }

    public void DisableSearch()
    {
        searchTextInputField.Text = "";
        searchTextInputField.Hide();
    }

    public bool IsSearchEnabled()
    {
        return searchTextInputField.isActiveAndEnabled;
    }
}
