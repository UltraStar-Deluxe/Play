using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class SongSelectSelectedSongDetailsControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongSelectSceneControl songSelectSceneControl;
    
    [Inject]
    private PlaylistManager playlistManager;
    
    [Inject]
    private SongAudioPlayer songAudioPlayer;
    
    [Inject]
    private Settings settings;
    
    [Inject]
    private SongSearchControl songSearchControl;
    
    [Inject]
    private UiManager uiManager;
    
    [Inject]
    private Statistics statistics;
    
    [Inject]
    private SongSelectPlayerListControl playerListControl;
    
    [Inject]
    private Injector injector;
    
    [Inject]
    private SongSelectSceneData sceneData;
    
    [Inject]
    private SceneNavigator sceneNavigator;
    
    [Inject(UxmlName = R.UxmlNames.localHighScoreContainer)]
    private VisualElement localHighScoreContainer;
    
    [Inject(UxmlName = R.UxmlNames.highscoresContainer)]
    private VisualElement highscoresContainer;

    [Inject(UxmlName = R.UxmlNames.highscoreTitleButton)]
    private Button highscoreTitleButton;
    
    [Inject(UxmlName = R.UxmlNames.selectedSongArtist)]
    private Label selectedSongArtist;

    [Inject(UxmlName = R.UxmlNames.selectedSongTitle)]
    private Label selectedSongTitle;

    [Inject(UxmlName = R.UxmlNames.selectedSongImageOuter)]
    private VisualElement selectedSongImageOuter;
    
    [Inject(UxmlName = R.UxmlNames.selectedSongImageInner)]
    private VisualElement selectedSongImageInner;
    
    [Inject(UxmlName = R.UxmlNames.songIndexLabel)]
    private Label songIndexLabel;

    [Inject(UxmlName = R.UxmlNames.songIndexContainer)]
    private VisualElement songIndexContainer;

    [Inject(UxmlName = R.UxmlNames.durationLabel)]
    private Label durationLabel;
    
    [Inject]
    private SongMetaManager songMetaManager;
    
    private SongMeta SelectedSong => songSelectSceneControl.SelectedSong;
    
    private readonly SongSelectSongRatingIconControl songRatingIconControl = new SongSelectSongRatingIconControl();
    
    public void OnInjectionFinished()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectSelectedSongDetailsControl.OnInjectionFinished");
        
        injector.Inject(songRatingIconControl);
        
        songIndexContainer.RegisterCallback<PointerDownEvent>(evt => songSearchControl.SetSearchText($"#{songSelectSceneControl.SelectedSongIndex + 1}"));

        highscoreTitleButton.RegisterCallbackButtonTriggered(_ => OpenHighScoreScene());
        
        songAudioPlayer.LoadedEventStream
            .Subscribe(_ => UpdateSongDurationLabel(songAudioPlayer.DurationOfSongInMillis));
        settings.ObserveEveryValueChanged(it => it.Difficulty)
            .Subscribe(_ =>
            {
                UpdateSongStatistics(songSelectSceneControl.SelectedSong);
                UpdateSongRatingIcons(songSelectSceneControl.SelectedSong);
            });
        
        // Smaller song index label if numbers get huge
        int songCount = songMetaManager.GetSongMetas().Count;
        if (songCount > 10000)
        {
            songIndexLabel.AddToClassList("tinyFont");
        }
        else if (songCount > 1000)
        {
            songIndexLabel.AddToClassList("smallFont");
        }
        else
        {
            songIndexLabel.AddToClassList("smallFont");
        }
        songIndexContainer.SetVisibleByDisplay(settings.ShowSongIndexInSongSelect);
    }

    private void OpenHighScoreScene()
    {
        SingingResultsSceneData singingResultsSceneData = new()
        {
            SongMetas = new List<SongMeta> { SelectedSong },
            partyModeSceneData = sceneData.partyModeSceneData,
            lastSceneData = sceneData,
        };
        sceneNavigator.LoadScene(EScene.SingingResultsScene, singingResultsSceneData);
    }

    private void UpdateSongRatingIcons(SongMeta selectedSong)
    {
        songRatingIconControl.UpdateSongRatingIcons(selectedSong, settings.Difficulty);
    }

    private void SetEmptySongDetails()
    {
        selectedSongArtist.text = "";
        selectedSongTitle.text = "";
        songIndexLabel.text = "";
        SongMetaImageUtils.SetDefaultSongImage(selectedSongImageOuter, selectedSongImageInner);
        songRatingIconControl.HideSongRatingIcons();
        UpdateSongStatistics(null);
    }

    private bool IsFavorite(SongMeta songMeta)
    {
        return songMeta != null
               && playlistManager.FavoritesPlaylist.HasSongEntry(songMeta);
    }
    
    public void OnSongSelectionChanged(SongSelection selection)
    {
        SongMeta selectedSong = selection.SongMeta;
        if (selectedSong == null)
        {
            SetEmptySongDetails();
            songIndexLabel.text = "-";
            return;
        }

        selectedSongArtist.text = selectedSong.Artist;
        selectedSongTitle.text = selectedSong.Title;
        SongMetaImageUtils.SetCoverOrBackgroundImage(selection.SongMeta, selectedSongImageInner, selectedSongImageOuter);
        songIndexLabel.text = $"{selection.SongIndex + 1} / {selection.SongsCount}";

        // The song duration requires loading the audio file.
        // Loading every song only to show its duration is slow (e.g. when scrolling through songs).
        // Instead, the label is updated when the AudioClip has been loaded.
        durationLabel.text = "";

        UpdateSongStatistics(selectedSong);

        UpdateSongRatingIcons(selectedSong);

        // Choose lyrics for duet song
        playerListControl.UpdateVoiceSelection();
    }

    private void UpdateSongDurationLabel(double durationInMillis)
    {
        int min = (int)Math.Floor(durationInMillis / 1000 / 60);
        int seconds = (int)Math.Floor((durationInMillis / 1000) % 60);
        durationLabel.text = $"{min}:{seconds.ToString().PadLeft(2, '0')}";
    }

    private void UpdateSongStatistics(SongMeta songMeta)
    {
        SongStatistics songStatistics = statistics.GetLocalStatistics(songMeta);
        if (songStatistics != null)
        {
            List<HighScoreEntry> topScores = songStatistics.HighScoreRecord.GetTopScores(1, settings.Difficulty);
            List<int> topScoreNumbers = topScores.Select(it => it.Score).ToList();

            UpdateTopScoreLabels(topScoreNumbers, localHighScoreContainer);
            highscoresContainer.SetVisibleByVisibility(!topScoreNumbers.IsNullOrEmpty());
        }
        else
        {
            UpdateTopScoreLabels(new List<int>(), localHighScoreContainer);
            highscoresContainer.HideByVisibility();
        }
    }

    private void UpdateTopScoreLabels(List<int> topScores, VisualElement labelContainer)
    {
        List<Label> labels = labelContainer.Query<Label>()
            .Where(label => !label.ClassListContains(R_PlayShared.UssClasses.fontIcon))
            .ToList();
        for (int i = 0; i < labels.Count; i++)
        {
            string scoreText = topScores.Count >= i + 1
                ? topScores[i].ToString()
                : "-";
            
            labels[i].text = scoreText;
        }
    }
}
