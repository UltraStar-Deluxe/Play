using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongPreviewController : MonoBehaviour, INeedInjection
{
    public float previewDelayInSeconds = 1;

    public float audioFadeInDurationInSeconds = 5;
    public float videoFadeInDurationInSeconds = 2;
    private float fadeInStartInSeconds;
    private float videoFadeInStartInSeconds;

    private bool isFadeInStarted;

    [Inject]
    private SongRouletteController songRouletteController;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongVideoPlayer songVideoPlayer;

    [Inject]
    private Settings settings;

    private SongMeta currentPreviewSongMeta;

    // The very first selected song should not be previewed.
    // The song is selected when opening the scene.
    private bool isFirstSelectedSong = true;

    void Start()
    {
        if (settings.AudioSettings.PreviewVolumePercent <= 0)
        {
            songVideoPlayer.gameObject.SetActive(false);
            songAudioPlayer.gameObject.SetActive(false);
            gameObject.SetActive(false);
            return;
        }

        songRouletteController.Selection.Subscribe(OnSelectedSongChanged);
    }

    void Update()
    {
        // Update fade-in of music volume and video transparency
        if (isFadeInStarted)
        {
            float audioPercent = (Time.time - fadeInStartInSeconds) / audioFadeInDurationInSeconds;
            audioPercent = NumberUtils.Limit(audioPercent, 0, 1);
            float maxVolume = settings.AudioSettings.PreviewVolumePercent / 100f;
            songAudioPlayer.audioPlayer.volume = audioPercent * maxVolume;

            // The video has an additional delay to load.
            // As long as no frame is ready yet, the VideoPlayer.time is 0.
            if (songVideoPlayer.videoPlayer.time <= 0)
            {
                videoFadeInStartInSeconds = Time.time;
            }
            float videoPercent = (Time.time - videoFadeInStartInSeconds) / videoFadeInDurationInSeconds;
            videoPercent = NumberUtils.Limit(videoPercent, 0, 1);
            songVideoPlayer.videoImage.SetAlpha(videoPercent);

            if (audioPercent >= 1 && videoPercent >= 1)
            {
                // Fade-in is complete
                isFadeInStarted = false;
            }
        }
    }

    private void OnSelectedSongChanged(SongSelection songSelection)
    {
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

        if (songMeta != null)
        {
            int previewStartInMillis = GetPreviewStartInMillis(songMeta);
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(previewDelayInSeconds, () => StartSongPreview(songMeta, previewStartInMillis)));
        }
        currentPreviewSongMeta = songMeta;
    }

    public int GetPreviewStartInMillis(SongMeta songMeta)
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

    private void StopSongPreview()
    {
        StopAllCoroutines();
        songAudioPlayer.PauseAudio();
        isFadeInStarted = false;
        songVideoPlayer.videoImageAndPlayerContainer.gameObject.SetActive(false);
    }

    private void StartSongPreview(SongMeta songMeta, int previewStartInMillis)
    {
        if (currentPreviewSongMeta != songMeta)
        {
            // A different song was selected in the meantime.
            return;
        }

        fadeInStartInSeconds = Time.time;
        videoFadeInStartInSeconds = Time.time;
        isFadeInStarted = true;
        StartAudioPreview(songMeta, previewStartInMillis);
        StartVideoPreview(songMeta);
    }

    private void StartVideoPreview(SongMeta songMeta)
    {
        if (songMeta.Video.IsNullOrEmpty())
        {
            return;
        }

        songVideoPlayer.videoImageAndPlayerContainer.gameObject.SetActive(true);
        songVideoPlayer.Init(songMeta, songAudioPlayer);
        songVideoPlayer.videoImage.SetAlpha(0);
    }

    private void StartAudioPreview(SongMeta songMeta, int previewStartInMillis)
    {
        songAudioPlayer.Init(songMeta);
        songAudioPlayer.PositionInSongInMillis = previewStartInMillis;
        songAudioPlayer.audioPlayer.volume = 0;
        songAudioPlayer.PlayAudio();
    }
}
