using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SongSelectSelectedSongDetailsControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongSelectSceneControl songSelectSceneControl;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private PlaylistManager playlistManager;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private Settings settings;

    [Inject]
    private GameObject gameObject;

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

    [Inject]
    private FocusableNavigator focusableNavigator;

    [Inject(UxmlName = R.UxmlNames.songListView)]
    private VisualElement songListView;

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
    private VisualElement entryIndexContainer;

    [Inject(UxmlName = R.UxmlNames.durationLabel)]
    private Label durationLabel;

    [Inject]
    private SongMetaManager songMetaManager;

    private SongMeta SelectedSong => songSelectSceneControl.SelectedSong;

    private readonly SongSelectSongRatingIconControl songRatingIconControl = new SongSelectSongRatingIconControl();

    private CancellationTokenSource setSongDetailsCoverOrBackgroundImageCancellationTokenSource;

    public void OnInjectionFinished()
    {
        using IDisposable d = ProfileMarkerUtils.Auto("SongSelectSelectedSongDetailsControl.OnInjectionFinished");

        injector.Inject(songRatingIconControl);

        entryIndexContainer.RegisterCallback<PointerDownEvent>(evt => songSearchControl.SetSearchText($"#{songRouletteControl.SelectedEntryIndex + 1}"));

        highscoreTitleButton.RegisterCallbackButtonTriggered(_ => OpenHighScoreScene());
        focusableNavigator.AddCustomNavigationTarget(highscoreTitleButton, Vector2.up, songListView);

        songAudioPlayer.LoadedEventStream
            .Subscribe(_ => UpdateSongDurationLabel(songAudioPlayer.DurationInMillis));
        settings.ObserveEveryValueChanged(it => it.Difficulty)
            .Subscribe(_ =>
            {
                UpdateHighScores(songSelectSceneControl.SelectedSong);
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

        settings.ObserveEveryValueChanged(_ => settings.ShowSongIndexInSongSelect)
            .Subscribe(newValue => entryIndexContainer.SetVisibleByDisplay(newValue))
            .AddTo(gameObject);
    }

    private void OpenHighScoreScene()
    {
        SingingResultsSceneData singingResultsSceneData = new()
        {
            SongMetas = new List<SongMeta> { SelectedSong },
            partyModeSceneData = sceneData.partyModeSceneData,
            lastSceneData = sceneData,
            GameRoundSettings = new(),
        };
        sceneNavigator.LoadScene(EScene.SingingResultsScene, singingResultsSceneData);
    }

    private void UpdateSongRatingIcons(SongMeta selectedSong)
    {
        songRatingIconControl.UpdateSongRatingIcons(selectedSong, settings.Difficulty);
    }

    private void SetEmptySongDetails()
    {
        selectedSongArtist.SetTranslatedText(Translation.Empty);
        selectedSongTitle.SetTranslatedText(Translation.Empty);
        songIndexLabel.SetTranslatedText(Translation.Empty);
        SongMetaImageUtils.SetDefaultSongImage(selectedSongImageOuter, selectedSongImageInner);
        songRatingIconControl.HideSongRatingIcons();
        UpdateHighScores(new List<HighScoreEntry>());
    }

    private bool IsFavorite(SongMeta songMeta)
    {
        return songMeta != null
               && playlistManager.FavoritesPlaylist.HasSongEntry(songMeta);
    }

    public void OnSongSelectionChanged(SongSelectEntrySelection selection)
    {
        SongSelectEntry selectedEntry = selection.Entry;
        if (selectedEntry is not SongSelectSongEntry songEntry)
        {
            SetEmptySongDetails();
            songIndexLabel.SetTranslatedText(Translation.Of("-"));
            return;
        }

        SongMeta selectedSong = songEntry.SongMeta;

        selectedSongArtist.SetTranslatedText(Translation.Of(selectedSong.Artist));
        selectedSongTitle.SetTranslatedText(Translation.Of(selectedSong.Title));
        songIndexLabel.SetTranslatedText(Translation.Of($"{selection.Index + 1} / {selection.Count}"));

        setSongDetailsCoverOrBackgroundImageCancellationTokenSource?.Cancel();
        setSongDetailsCoverOrBackgroundImageCancellationTokenSource = new CancellationTokenSource();
        SongMetaImageUtils.SetCoverOrBackgroundImageAsync(
            setSongDetailsCoverOrBackgroundImageCancellationTokenSource.Token,
            selectedSong,
            selectedSongImageInner,
            selectedSongImageOuter);

        // The song duration requires loading the audio file.
        // Loading every song only to show its duration is slow (e.g. when scrolling through songs).
        // Instead, the label is updated when the AudioClip has been loaded.
        durationLabel.SetTranslatedText(Translation.Empty);

        UpdateHighScores(selectedSong);

        UpdateSongRatingIcons(selectedSong);

        // Choose lyrics for duet song
        playerListControl.UpdateVoiceSelection();
    }

    private void UpdateSongDurationLabel(double durationInMillis)
    {
        int min = (int)Math.Floor(durationInMillis / 1000 / 60);
        int seconds = (int)Math.Floor((durationInMillis / 1000) % 60);
        durationLabel.SetTranslatedText(Translation.Of($"{min}:{seconds.ToString().PadLeft(2, '0')}"));
    }

    private void UpdateHighScores(SongMeta songMeta)
    {
        List<HighScoreEntry> highScoreEntries = StatisticsUtils.GetLocalHighScoreEntries(statistics, songMeta);
        UpdateHighScores(highScoreEntries);
    }

    private void UpdateHighScores(List<HighScoreEntry> highScoreEntries)
    {
        if (highScoreEntries.IsNullOrEmpty())
        {
            UpdateTopScoreLabels(new List<int>(), localHighScoreContainer);
            highscoresContainer.HideByVisibility();
            return;
        }

        List<HighScoreEntry> topScores = StatisticsUtils.GetTopScores(
            highScoreEntries,
            1,
            settings.Difficulty);
        List<int> topScoreNumbers = topScores.Select(it => it.Score).ToList();

        UpdateTopScoreLabels(topScoreNumbers, localHighScoreContainer);
        highscoresContainer.SetVisibleByVisibility(!topScoreNumbers.IsNullOrEmpty());
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

            labels[i].SetTranslatedText(Translation.Of(scoreText));
        }
    }
}
