using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongPreviewControl : MonoBehaviour, INeedInjection
{
    public float PreviewDelayInSeconds { get; set; } = 1;
    public float AudioFadeInDurationInSeconds { get; set; } = 5;
    public float VideoFadeInDurationInSeconds { get; set; } = 2;

    protected float fadeInStartTimeInSeconds;
    protected float videoFadeInStartTimeInSeconds;
    protected bool isFadeInStarted;

    [Inject]
    protected SongAudioPlayer songAudioPlayer;

    [Inject(Optional = true)]
    protected SongVideoPlayer songVideoPlayer;

    [Inject]
    protected Settings settings;

    protected SongMeta currentPreviewSongMeta;

    protected readonly Subject<SongMeta> startSongPreviewEventStream = new();
    public IObservable<SongMeta> StartSongPreviewEventStream => startSongPreviewEventStream;

    protected readonly Subject<SongMeta> stopSongPreviewEventStream = new();
    public IObservable<SongMeta> StopSongPreviewEventStream => stopSongPreviewEventStream;

    public ReactiveProperty<float> VideoFadeIn { get; private set; } = new();
    public ReactiveProperty<float> BackgroundImageFadeIn { get; private set; } = new();

    protected virtual void Start()
    {
        if (GetFinalPreviewVolume() <= 0)
        {
            if (songVideoPlayer != null)
            {
                songVideoPlayer.gameObject.SetActive(false);
            }
            songAudioPlayer.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }

    protected virtual void Update()
    {
        // Update fade-in of music volume and video transparency
        if (isFadeInStarted)
        {
            float audioFadeInPercent = UpdateAudioFadeIn();
            float videoFadeInPercent = UpdateVideoFadeIn();

            if ((audioFadeInPercent < 0 || audioFadeInPercent >= 1)
                && (videoFadeInPercent < 0 || videoFadeInPercent >= 1))
            {
                // Fade-in is complete
                isFadeInStarted = false;
            }
        }
    }

    protected virtual float UpdateVideoFadeIn()
    {
        if (songVideoPlayer == null)
        {
            return -1;
        }

        // The video has an additional delay to load.
        // As long as no frame is ready yet, the VideoPlayer.time is 0.
        if (songVideoPlayer.HasLoadedVideo && songVideoPlayer.videoPlayer.time <= 0)
        {
            videoFadeInStartTimeInSeconds = Time.time;
        }

        float videoFadeInPercent = (Time.time - videoFadeInStartTimeInSeconds) / VideoFadeInDurationInSeconds;
        videoFadeInPercent = NumberUtils.Limit(videoFadeInPercent, 0, 1);
        if (songVideoPlayer.HasLoadedVideo)
        {
            VideoFadeIn.Value = videoFadeInPercent;
        }
        else if (songVideoPlayer.HasLoadedBackgroundImage)
        {
            BackgroundImageFadeIn.Value = videoFadeInPercent;
        }

        return videoFadeInPercent;
    }

    protected virtual float UpdateAudioFadeIn()
    {
        float audioFadeInPercent = (Time.time - fadeInStartTimeInSeconds) / AudioFadeInDurationInSeconds;
        audioFadeInPercent = NumberUtils.Limit(audioFadeInPercent, 0, 1);
        float maxVolume = GetFinalPreviewVolume();
        songAudioPlayer.audioPlayer.volume = audioFadeInPercent * maxVolume;

        return audioFadeInPercent;
    }

    public virtual void StartSongPreview(SongMeta songMeta)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        StopSongPreview();

        if (songMeta == currentPreviewSongMeta)
        {
            return;
        }

        currentPreviewSongMeta = songMeta;
        if (songMeta != null)
        {
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(PreviewDelayInSeconds, () => DoStartSongPreview(songMeta)));
        }
    }

    protected virtual int GetPreviewStartInMillis(SongMeta songMeta)
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

    public virtual void StopSongPreview()
    {
        StopAllCoroutines();
        songAudioPlayer.PauseAudio();
        isFadeInStarted = false;
        stopSongPreviewEventStream.OnNext(currentPreviewSongMeta);
    }

    protected virtual void DoStartSongPreview(SongMeta songMeta)
    {
        if (songMeta == null
            || currentPreviewSongMeta != songMeta)
        {
            // A different song was selected in the meantime.
            return;
        }

        fadeInStartTimeInSeconds = Time.time;
        videoFadeInStartTimeInSeconds = Time.time;
        isFadeInStarted = true;
        int previewStartInMillis = GetPreviewStartInMillis(songMeta);
        StartAudioPreview(songMeta, previewStartInMillis);
        StartVideoPreview(songMeta);

        startSongPreviewEventStream.OnNext(songMeta);
    }

    protected virtual void StartVideoPreview(SongMeta songMeta)
    {
        if (songMeta.Video.IsNullOrEmpty()
            || songVideoPlayer == null)
        {
            return;
        }

        VideoFadeIn.Value = 0;
        BackgroundImageFadeIn.Value = 0;

        songVideoPlayer.SongMeta = songMeta;
        songVideoPlayer.StartVideoOrShowBackgroundImage();
    }

    protected virtual void StartAudioPreview(SongMeta songMeta, int previewStartInMillis)
    {
        try
        {
            songAudioPlayer.Init(songMeta);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            string errorMessage = $"Audio could not be loaded (artist: {songMeta.Artist}, title: {songMeta.Title})";
            UiManager.CreateNotification(errorMessage);
            return;
        }
        
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
            UiManager.CreateNotification(errorMessage, "error");
        }
    }

    protected virtual float GetFinalPreviewVolume()
    {
        return settings.AudioSettings.PreviewVolumePercent / 100.0f;
    }
}
