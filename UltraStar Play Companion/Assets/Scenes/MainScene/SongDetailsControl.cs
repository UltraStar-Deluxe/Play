using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongDetailsControl : INeedInjection, IInjectionFinishedListener, IDisposable
{
    [Inject]
    private Settings settings;
    
    [Inject]
    private MainGameHttpClient mainGameHttpClient;

    [Inject(UxmlName = R.UxmlNames.songListContainer)]
    private VisualElement songListContainer;
    
    [Inject(UxmlName = R.UxmlNames.songDetailsContainer)]
    private VisualElement songDetailsContainer;
    
    [Inject(UxmlName = R.UxmlNames.songArtistLabel)]
    private Label songArtistLabel;
    
    [Inject(UxmlName = R.UxmlNames.songTitleLabel)]
    private Label songTitleLabel;
    
    [Inject(UxmlName = R.UxmlNames.songImage)]
    private VisualElement songImage;
    
    [Inject(UxmlName = R.UxmlNames.backButton)]
    private Button backButton;
        
    [Inject(UxmlName = R.UxmlNames.favoriteButton)]
    private Button favoriteButton;
    
    [Inject(UxmlName = R.UxmlNames.enqueueButton)]
    private Button enqueueButton;
    
    [Inject(UxmlName = R.UxmlNames.toggleLyricsButton)]
    private Button toggleLyricsButton;
    
    [Inject(UxmlName = R.UxmlNames.lyricsLabel)]
    private Label lyricsLabel;
    
    private SongDto songDto;
    public SongDto SongDto
    {
        get
        {
            return songDto;
        }
        set
        {
            songDto = value;
            UpdateControls();
        }
    }

    private bool isShowLyrics;

    private Texture2D texture2D;
    
    public void OnInjectionFinished()
    {
        HideSongDetails();
        backButton.RegisterCallbackButtonTriggered(() => HideSongDetails());
        favoriteButton.RegisterCallbackButtonTriggered(() => ToggleFavorite());
        enqueueButton.RegisterCallbackButtonTriggered(() => EnqueueSong());
        toggleLyricsButton.RegisterCallbackButtonTriggered(() => ToggleLyrics());
    }

    private void ToggleLyrics()
    {
        isShowLyrics = !isShowLyrics;
        if (isShowLyrics)
        {
            lyricsLabel.AddToClassList("showLyrics");
        }
        else
        {
            lyricsLabel.RemoveFromClassList("showLyrics");
        }
    }

    private void EnqueueSong()
    {
        HideSongDetails();
    }

    private void ToggleFavorite()
    {
    }

    private void UpdateControls()
    {
        if (songDto == null)
        {
            return;
        }
        songArtistLabel.text = songDto.Artist;
        songTitleLabel.text = songDto.Title;

        LoadSongDetails();
        LoadSongImage();
    }

    private void LoadSongImage()
    {
        songImage.HideByVisibility();
        
        mainGameHttpClient.GetRequest($"api/rest/songImage/{songDto.Hash}",
            response =>
            {
                ImageDto imageDto = JsonConverter.FromJson<ImageDto>(response, false);
                if (imageDto == null
                    || imageDto.JpgBytesBase64.IsNullOrEmpty())
                {
                    Debug.LogError($"Failed to load image for song {songDto.Artist} - {songDto.Title}. Response: {response}");
                    return;
                }
                songImage.ShowByVisibility();
                byte[] jpgBytes = Convert.FromBase64String(imageDto.JpgBytesBase64);
                
                // Remove old texture if any
                GameObject.Destroy(texture2D);
                
                // Load new texture from bytes
                texture2D = new Texture2D(2, 2);
                // This will auto-resize the texture dimensions.
                texture2D.LoadImage(jpgBytes);

                songImage.style.backgroundImage = new StyleBackground(texture2D);
            });
    }

    private void LoadSongDetails()
    {
        lyricsLabel.text = "Loading lyrics...";
        
        mainGameHttpClient.GetRequest($"api/rest/song/{songDto.Hash}",
            response =>
            {
                SongDetailsDto songDetailsDto = JsonConverter.FromJson<SongDetailsDto>(response, false);
                if (songDetailsDto == null
                    || songDetailsDto.SongId.IsNullOrEmpty())
                {
                    Debug.LogError($"Failed to load details for song {songDto.Artist} - {songDto.Title}. Response: {response}");
                    return;
                }
                UpdateLyrics(songDetailsDto.VoiceNameToLyricsMap);
            });
    }

    private void UpdateLyrics(Dictionary<string,string> voiceNameToLyricsMap)
    {
        if (voiceNameToLyricsMap.Count <= 1)
        {
            lyricsLabel.text = voiceNameToLyricsMap.Values.FirstOrDefault();
            return;
        }

        string GetVoiceDisplayName(string voiceName)
        {
            if (voiceName.IsNullOrEmpty()
                || voiceName == "P1")
            {
                return "Vocals 1";
            }
            else if (voiceName == "P2")
            {
                return "Vocals 2";
            }

            return voiceName;
        }
        
        string lyricsWithVoiceNames = voiceNameToLyricsMap
            .Select(entry => GetVoiceDisplayName(entry.Key) + ": " + entry.Value)
            .JoinWith("\n\n");
        lyricsLabel.text = lyricsWithVoiceNames;
    }

    public void ShowSongDetails(SongDto newSongDto)
    {
        SongDto = newSongDto;
        songDetailsContainer.ShowByDisplay();
        songListContainer.HideByDisplay();
    }

    public void HideSongDetails()
    {
        songDetailsContainer.HideByDisplay();
        songListContainer.ShowByDisplay();
    }

    public void Dispose()
    {
        GameObject.Destroy(texture2D);
    }
}
