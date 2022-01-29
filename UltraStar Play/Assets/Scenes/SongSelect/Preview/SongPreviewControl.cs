using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongPreviewControl : MonoBehaviour, INeedInjection
{
    public float previewDelayInSeconds = 1;

    public float audioFadeInDurationInSeconds = 5;
    public float videoFadeInDurationInSeconds = 2;
    private float fadeInStartInSeconds;
    private float videoFadeInStartInSeconds;

    private bool isFadeInStarted;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private SongRouletteControl songRouletteControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongVideoPlayer songVideoPlayer;

    [Inject]
    private Settings settings;

    private SongMeta currentPreviewSongMeta;
    private SongEntryControl currentSongEntryControl;

    // The very first selected song should not be previewed.
    // The song is selected when opening the scene.
    private bool isFirstSelectedSong = true;

    private void Start()
    {
        if (GetFinalPreviewVolume() <= 0)
        {
            songVideoPlayer.gameObject.SetActive(false);
            songAudioPlayer.gameObject.SetActive(false);
            gameObject.SetActive(false);
            return;
        }

        songRouletteControl.Selection.Subscribe(StartSongPreview);
    }

    private void Update()
    {
        // Update fade-in of music volume and video transparency
        if (isFadeInStarted)
        {
            float audioPercent = (Time.time - fadeInStartInSeconds) / audioFadeInDurationInSeconds;
            audioPercent = NumberUtils.Limit(audioPercent, 0, 1);
            float maxVolume = GetFinalPreviewVolume();
            songAudioPlayer.audioPlayer.volume = audioPercent * maxVolume;

            // The video has an additional delay to load.
            // As long as no frame is ready yet, the VideoPlayer.time is 0.
            if (songVideoPlayer.HasLoadedVideo && songVideoPlayer.videoPlayer.time <= 0)
            {
                videoFadeInStartInSeconds = Time.time;
            }
            float videoPercent = (Time.time - videoFadeInStartInSeconds) / videoFadeInDurationInSeconds;
            videoPercent = NumberUtils.Limit(videoPercent, 0, 1);
            if (songVideoPlayer.HasLoadedVideo
                && currentSongEntryControl != null)
            {
                currentSongEntryControl.SongPreviewVideoImage.SetBackgroundImageAlpha(videoPercent);
            }
            else if (songVideoPlayer.HasLoadedBackgroundImage
                     && currentSongEntryControl != null)
            {
                currentSongEntryControl.SongPreviewBackgroundImage.SetBackgroundImageAlpha(videoPercent);
            }

            if (audioPercent >= 1 && videoPercent >= 1)
            {
                // Fade-in is complete
                isFadeInStarted = false;
            }
        }
    }

    public void StartSongPreview(SongSelection songSelection)
    {
        if (songRouletteControl.IsDrag
            || songRouletteControl.IsFlickGesture)
        {
            return;
        }

        StopSongPreview();

        if (songSelection.SongIndex != 0)
        {
            isFirstSelectedSong = false;
        }

        SongMeta songMeta = songSelection.SongMeta;
        if (songMeta == currentPreviewSongMeta)
        {
            return;
        }

        if (isFirstSelectedSong)
        {
            return;
        }

        currentPreviewSongMeta = songMeta;
        currentSongEntryControl = songRouletteControl.SongEntryControls
            .FirstOrDefault(it => it.SongMeta == songMeta);
        if (songMeta != null)
        {
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(previewDelayInSeconds, () => DoStartSongPreview(songMeta)));
        }
    }

    private int GetPreviewStartInMillis(SongMeta songMeta)
    {
        if (songMeta.PreviewStart > 0)
        {
            return (int)(songMeta.PreviewStart * 1000);
        }

        // Fallback: find some lyrics approx. 1/3 into the song.
        Voice voice = songMeta.GetVoices().FirstOrDefault();
        if (voice == null)
        {
            return 0;
        }

        List<Note> notes = voice.Sentences.SelectMany(sentence => sentence.Notes).ToList();
        if (notes.Count == 0)
        {
            return 0;
        }

        int noteIndex = (int)((notes.Count - 1) * 0.33f);
        Note note = notes[noteIndex];
        int noteStartBeatInMillis = (int)BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat);
        return noteStartBeatInMillis;
    }

    public void StopSongPreview()
    {
        StopAllCoroutines();
        songAudioPlayer.PauseAudio();
        isFadeInStarted = false;
        songRouletteControl.SongEntryControls.ForEach(it => it.SongPreviewVideoImage.HideByDisplay());
        songRouletteControl.SongEntryControls.ForEach(it => it.SongPreviewBackgroundImage.HideByDisplay());
    }

    private void DoStartSongPreview(SongMeta songMeta)
    {
        if (songMeta == null
            || currentPreviewSongMeta != songMeta)
        {
            // A different song was selected in the meantime.
            return;
        }

        fadeInStartInSeconds = Time.time;
        videoFadeInStartInSeconds = Time.time;
        isFadeInStarted = true;
        int previewStartInMillis = GetPreviewStartInMillis(songMeta);
        StartAudioPreview(songMeta, previewStartInMillis);
        StartVideoPreview(songMeta);
    }

    private void StartVideoPreview(SongMeta songMeta)
    {
        if (songMeta.Video.IsNullOrEmpty()
            || currentSongEntryControl == null)
        {
            return;
        }

        currentSongEntryControl.SongPreviewVideoImage.ShowByDisplay();
        currentSongEntryControl.SongPreviewVideoImage.SetBackgroundImageAlpha(0);
        currentSongEntryControl.SongPreviewBackgroundImage.ShowByDisplay();
        currentSongEntryControl.SongPreviewBackgroundImage.SetBackgroundImageAlpha(0);

        songVideoPlayer.SongMeta = songMeta;
        songVideoPlayer.StartVideoOrShowBackgroundImage();
    }

    private void StartAudioPreview(SongMeta songMeta, int previewStartInMillis)
    {
        songAudioPlayer.Init(songMeta);
        songAudioPlayer.PositionInSongInMillis = previewStartInMillis;
        songAudioPlayer.audioPlayer.volume = 0;
        if (songAudioPlayer.HasAudioClip)
        {
            songAudioPlayer.PlayAudio();
        }
        else
        {
            string errorMessage = $"Audio could not be loaded (artist: {songMeta.Artist}, title: {songMeta.Title})";
            Debug.LogError(errorMessage);
            uiManager.CreateNotificationVisualElement(errorMessage, "error");
        }
    }

    private float GetFinalPreviewVolume()
    {
        return (settings.AudioSettings.PreviewVolumePercent / 100.0f) * (settings.AudioSettings.VolumePercent / 100.0f);
    }
}
