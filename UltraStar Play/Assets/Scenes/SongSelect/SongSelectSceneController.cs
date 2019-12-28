using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Linq;
using UniRx;

public class SongSelectSceneController : MonoBehaviour, IOnHotSwapFinishedListener
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

    public SongSelectPlayerProfileListController playerProfileListController;

    private SearchInputField searchTextInputField;

    private SongRouletteController songRouletteController;

    private SongSelectSceneData sceneData;
    private List<SongMeta> songMetas;

    private SongMeta selectedSongBeforeSearch;

    public GameObject noSongsFoundMessage;

    private SongMeta SelectedSong
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
        List<PlayerProfile> playerProfiles = SettingsManager.Instance.Settings.PlayerProfiles;

        songRouletteController = FindObjectOfType<SongRouletteController>();
        songRouletteController.SongSelectSceneController = this;

        InitSongRoulette();

        // Show a message when no songs have been found.
        noSongsFoundMessage.SetActive(songMetas.IsNullOrEmpty());
    }

    public void OnHotSwapFinished()
    {
        InitSongRoulette();
    }

    private void InitSongRoulette()
    {
        songRouletteController.SetSongs(songMetas);
        if (sceneData.SongMeta != null)
        {
            songRouletteController.SelectSong(sceneData.SongMeta);
        }

        songRouletteController.Selection.Subscribe(newValue => OnNewSongSelection(newValue));
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

        bool isDuet = selectedSong.VoiceNames.Count > 1;
        duetIndicator.SetActive(isDuet);
    }

    private SingSceneData CreateSingSceneData(SongMeta songMeta)
    {
        SingSceneData singSceneData = new SingSceneData();
        singSceneData.SelectedSongMeta = songMeta;

        List<PlayerProfile> selectedPlayerProfiles = playerProfileListController.GetSelectedPlayerProfiles();
        if (selectedPlayerProfiles.IsNullOrEmpty())
        {
            UiManager.Instance.CreateWarningDialog("No player selected", "Select a player profile for singing.\n New player profiles can be create in the settings.");
            return null;
        }
        singSceneData.SelectedPlayerProfiles = selectedPlayerProfiles;

        PlayerProfileToMicProfileMap playerProfileToMicProfileMap = playerProfileListController.GetSelectedPlayerProfileToMicProfileMap();
        singSceneData.PlayerProfileToMicProfileMap = playerProfileToMicProfileMap;
        return singSceneData;
    }

    private void StartSingScene(SongMeta songMeta)
    {
        SingSceneData singSceneData = CreateSingSceneData(songMeta);
        if (singSceneData != null)
        {
            SceneNavigator.Instance.LoadScene(EScene.SingScene, singSceneData);
        }
    }

    private void StartSongEditorScene(SongMeta songMeta)
    {
        SongEditorSceneData editorSceneData = new SongEditorSceneData();
        editorSceneData.SelectedSongMeta = songMeta;

        SingSceneData singSceneData = CreateSingSceneData(songMeta);
        if (singSceneData != null)
        {
            editorSceneData.PreviousSceneData = singSceneData;
            editorSceneData.PreviousScene = EScene.SingScene;
        }
        else
        {
            editorSceneData.PreviousSceneData = sceneData;
            editorSceneData.PreviousScene = EScene.SongSelectScene;
        }

        SceneNavigator.Instance.LoadScene(EScene.SongEditorScene, editorSceneData);
    }

    private SongSelectSceneData CreateDefaultSceneData()
    {
        SongSelectSceneData sceneData = new SongSelectSceneData();
        return sceneData;
    }

    private void SetEmptySongDetails()
    {
        artistText.SetText("");
        songTitleText.text = "";
        songCountText.text = "0/0";
        videoIndicator.SetActive(false);
        duetIndicator.SetActive(false);
    }

    public void OnRandomSong()
    {
        songRouletteController.SelectRandomSong();
    }

    public void OnNextSong()
    {
        songRouletteController.SelectNextSong();
    }

    public void OnPreviousSong()
    {
        songRouletteController.SelectPreviousSong();
    }

    public void StartSingScene()
    {
        if (SelectedSong != null)
        {
            StartSingScene(SelectedSong);
        }
    }

    public void StartSongEditorScene()
    {
        if (SelectedSong != null)
        {
            StartSongEditorScene(SelectedSong);
        }
    }

    public void OnSearchTextChanged()
    {
        SongMeta lastSelectedSong = SelectedSong;
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
        selectedSongBeforeSearch = SelectedSong;

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
