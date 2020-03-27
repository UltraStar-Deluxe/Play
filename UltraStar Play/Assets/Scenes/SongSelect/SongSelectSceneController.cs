using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;
using System.Linq;
using UniRx;
using UniInject;
using UnityEngine.EventSystems;
using System.Globalization;
using System.Threading;
using System.IO;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongSelectSceneController : MonoBehaviour, IOnHotSwapFinishedListener, INeedInjection, IBinder
{
    public static SongSelectSceneController Instance
    {
        get
        {
            return FindObjectOfType<SongSelectSceneController>();
        }
    }

    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongVideoPlayer songVideoPlayer;

    [InjectedInInspector]
    public SongRouletteController songRouletteController;

    public ArtistText artistText;
    public Text songTitleText;
    public Text songCountText;
    public GameObject videoIndicator;
    public GameObject duetIndicator;

    public SongSelectPlayerProfileListController playerProfileListController;

    private SearchInputField searchTextInputField;

    private SongSelectSceneData sceneData;
    private List<SongMeta> songMetas;
    private int lastSongMetasReloadFrame = -1;
    private Statistics statsManager;

    private SongMeta selectedSongBeforeSearch;

    [Inject]
    private EventSystem eventSystem;

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
        SongMetaManager.Instance.ScanFilesIfNotDoneYet();
        // Give the song search some time, otherwise the "no songs found" label flickers once.
        if (!SongMetaManager.IsSongScanFinished)
        {
            Thread.Sleep(100);
        }

        sceneData = SceneNavigator.Instance.GetSceneData(CreateDefaultSceneData());

        searchTextInputField = GameObjectUtils.FindObjectOfType<SearchInputField>(true);

        GetSongMetasFromManager();

        songRouletteController.SelectionClickedEventStream
            .Subscribe(selection => CheckAudioAndStartSingScene());

        statsManager = StatsManager.Instance.Statistics;

        InitSongRoulette();

        // Show a message when no songs have been found.
        noSongsFoundMessage.SetActive(songMetas.IsNullOrEmpty());
    }

    private void GetSongMetasFromManager()
    {
        songMetas = new List<SongMeta>(SongMetaManager.Instance.GetSongMetas());
        songMetas.Sort((songMeta1, songMeta2) => string.Compare(songMeta1.Artist, songMeta2.Artist, true, CultureInfo.InvariantCulture));
        noSongsFoundMessage.SetActive(songMetas.IsNullOrEmpty());
    }

    void Update()
    {
        // Check if new songs were loaded in background. Update scene if necessary.
        if (songMetas.Count != SongMetaManager.Instance.GetSongMetas().Count
            && !IsSearchEnabled()
            && lastSongMetasReloadFrame + 10 < Time.frameCount)
        {
            GetSongMetasFromManager();
            SongMeta selectedSong = songRouletteController.Selection.Value.SongMeta;
            InitSongRoulette();
            songRouletteController.SelectSong(selectedSong);
        }
    }

    public void OnHotSwapFinished()
    {
        InitSongRoulette();
    }

    private void InitSongRoulette()
    {
        lastSongMetasReloadFrame = Time.frameCount;
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

    public void JumpToSongWhereTitleStartsWith(string text)
    {
        string textToLower = text.ToLowerInvariant();
        SongMeta match = songRouletteController.Find(it => it.Title.ToLowerInvariant().StartsWith(textToLower));
        if (match != null)
        {
            songRouletteController.SelectSong(match);
        }
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

    public void CheckAudioAndStartSingScene()
    {
        if (SelectedSong != null)
        {
            // Check that the audio file exists
            string audioPath = SongMetaUtils.GetAbsoluteSongFilePath(SelectedSong);
            if (!File.Exists(audioPath))
            {
                UiManager.Instance.CreateWarningDialog("Audio Error", "Audio file does not exist: " + audioPath);
                return;
            }

            // Check that the used audio format can be loaded.
            songAudioPlayer.Init(SelectedSong);
            if (!songAudioPlayer.HasAudioClip)
            {
                UiManager.Instance.CreateWarningDialog("Audio Error", "Audio file could not be loaded.\nPlease use a supported format.");
                return;
            }

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
            DoSearch((songMeta) =>
            {
                bool titleMatches = songMeta.Title.ToLower().Contains(searchText);
                bool artistMatches = songMeta.Artist.ToLower().Contains(searchText);
                return titleMatches || artistMatches;
            });
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

    public bool IsSearchTextInputHasFocus()
    {
        return eventSystem.currentSelectedGameObject == searchTextInputField.GetInputField().gameObject;
    }

    public void ToggleSelectedPlayers()
    {
        playerProfileListController.ToggleSelectedPlayers();
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(songRouletteController);
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songVideoPlayer);
        return bb.GetBindings();
    }
}
