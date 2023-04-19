using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class SongSelectSelectedSongDetailsControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(defaultSongImage))]
    private Sprite defaultSongImage;
    
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
    
    [Inject(UxmlName = R.UxmlNames.localHighScoreContainer)]
    private VisualElement localHighScoreContainer;

    [Inject(UxmlName = R.UxmlNames.selectedSongArtist)]
    private Label selectedSongArtist;

    [Inject(UxmlName = R.UxmlNames.selectedSongTitle)]
    private Label selectedSongTitle;

    [Inject(UxmlName = R.UxmlNames.selectedSongImageOuter)]
    private VisualElement selectedSongImageOuter;

    [Inject(UxmlName = R.UxmlNames.selectedSongImageInner)]
    private VisualElement selectedSongImageInner;
    
    [Inject(UxmlName = R.UxmlNames.showLyricsButton)]
    private Button showLyricsButton;
    
    [Inject(UxmlName = R.UxmlNames.songIndexLabel)]
    private Label songIndexLabel;

    [Inject(UxmlName = R.UxmlNames.songIndexContainer)]
    private VisualElement songIndexContainer;

    [Inject(UxmlName = R.UxmlNames.durationLabel)]
    private Label durationLabel;
    
    [Inject(UxmlName = R.UxmlNames.noFavoriteIcon)]
    private MaterialIcon noFavoriteIcon;

    [Inject(UxmlName = R.UxmlNames.favoriteIcon)]
    private MaterialIcon favoriteIcon;

    [Inject(UxmlName = R.UxmlNames.toggleFavoriteButton)]
    private Button toggleFavoriteButton;

    private MessageDialogControl lyricsDialogControl;

    private SongMeta SelectedSong => songSelectSceneControl.SelectedSong;
    
    public void OnInjectionFinished()
    {
        showLyricsButton.RegisterCallbackButtonTriggered(_ => ShowLyricsPopup());
        
        toggleFavoriteButton.RegisterCallbackButtonTriggered(_ => songSelectSceneControl.ToggleSelectedSongIsFavorite());
        songIndexContainer.RegisterCallback<PointerDownEvent>(evt => songSearchControl.SetSearchText($"#{songSelectSceneControl.SelectedSongIndex + 1}"));

        playlistManager.PlaylistChangeEventStream
            .Subscribe(_ => UpdateFavoriteIcon());
        songAudioPlayer.LoadedEventStream
            .Subscribe(_ => UpdateSongDurationLabel(songAudioPlayer.DurationOfSongInMillis));
        settings.ObserveEveryValueChanged(it => it.GameSettings.Difficulty)
            .Subscribe(_ => UpdateSongStatistics(songSelectSceneControl.SelectedSong));
    }
    
    private void ShowLyricsPopup()
    {
        if (lyricsDialogControl != null
            || SelectedSong == null)
        {
            return;
        }

        lyricsDialogControl = uiManager.CreateDialogControl($"{SelectedSong.Title}");
        lyricsDialogControl.DialogClosedEventStream.Subscribe(_ => lyricsDialogControl = null);
        
        Label CreateLyricsLabel(string lyrics)
        {
            Label lyricsLabel = new Label(lyrics);
            lyricsLabel.enableRichText = true;
            lyricsLabel.AddToClassList("songSelectLyricsPreview");
            return lyricsLabel;
        }
        
        if (SelectedSong.GetVoices().Count < 2)
        {
            string lyrics = SongMetaUtils.GetLyrics(SelectedSong, Voice.firstVoiceName);
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(lyrics));
        }
        else
        {
            string firstVoiceLyrics = $"<i><b>{SelectedSong.VoiceNames.FirstOrDefault().Value}</b></i>\n\n" 
                                      + SongMetaUtils.GetLyrics(SelectedSong, Voice.firstVoiceName);
            string secondVoiceLyrics = $"<i><b>{SelectedSong.VoiceNames.LastOrDefault().Value}</b></i>\n\n" 
                                       + SongMetaUtils.GetLyrics(SelectedSong, Voice.secondVoiceName);
            
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(firstVoiceLyrics));
            lyricsDialogControl.AddVisualElement(CreateLyricsLabel(secondVoiceLyrics));
        }
        
        // Add attribution and license info
        AccordionItem attributionAccordionItem = new("Attribution");
        attributionAccordionItem.Add(AttributionUtils.CreateAttributionVisualElement(SelectedSong));
        lyricsDialogControl.AddVisualElement(attributionAccordionItem);
        
        ThemeManager.ApplyThemeSpecificStylesToVisualElements(lyricsDialogControl.DialogRootVisualElement);
    }
    
    private void SetEmptySongDetails()
    {
        selectedSongArtist.text = "";
        selectedSongTitle.text = "";
        songIndexLabel.text = "";
        selectedSongImageOuter.style.backgroundImage = new StyleBackground(defaultSongImage);
        selectedSongImageInner.style.backgroundImage = new StyleBackground(defaultSongImage);
        UpdateFavoriteIcon();
        UpdateSongStatistics(null);
    }

    private void UpdateFavoriteIcon()
    {
        bool isFavorite = IsFavorite(songSelectSceneControl.SelectedSong);
        favoriteIcon.SetVisibleByDisplay(isFavorite);
        noFavoriteIcon.SetVisibleByDisplay(!isFavorite);
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

        UpdateFavoriteIcon();

        UpdateSongStatistics(selectedSong);

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
        LocalStatistic localStatistic = statistics.GetLocalStats(songMeta);
        if (localStatistic != null)
        {
            List<SongStatistic> topScores = localStatistic.StatsEntries.GetTopScores(1, settings.GameSettings.Difficulty);
            List<int> topScoreNumbers = topScores.Select(it => it.Score).ToList();

            UpdateTopScoreLabels(topScoreNumbers, localHighScoreContainer);
        }
        else
        {
            UpdateTopScoreLabels(new List<int>(), localHighScoreContainer);
        }
    }

    private void UpdateTopScoreLabels(List<int> topScores, VisualElement labelContainer)
    {
        List<Label> labels = labelContainer.Query<Label>().ToList();
        for (int i = 0; i < labels.Count; i++)
        {
            string scoreText = topScores.Count >= i + 1
                ? topScores[i].ToString()
                : "-";
            
            labels[i].text = scoreText;
        }
    }
}
