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
    public int PreviewDelayInMillis { get; set; } = -1;

    public float AudioFadeInDurationInSeconds { get; set; } = 2;
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

    // TODO: Inject the instance
    private WebViewManager WebViewManager => WebViewManager.Instance;

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
            float audioFadeInFactor = UpdateAudioFadeIn();
            float videoFadeInFactor = UpdateVideoFadeIn();

            if ((audioFadeInFactor < 0 || audioFadeInFactor >= 1)
                && (videoFadeInFactor < 0 || videoFadeInFactor >= 1))
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
        if (!songVideoPlayer.IsPartiallyLoaded
            || (songVideoPlayer.PositionInMillis <= 0
                // WebView must be visible to see controls, even when audio is not ready yet.
                && songVideoPlayer.CurrentVideoSupportProvider is not WebViewVideoSupportProvider))
        {
            videoFadeInStartTimeInSeconds = Time.time;
        }

        float videoFadeInPercent = (Time.time - videoFadeInStartTimeInSeconds) / Math.Max(VideoFadeInDurationInSeconds, 0.001f);
        videoFadeInPercent = NumberUtils.Limit(videoFadeInPercent, 0, 1);
        VideoFadeIn.Value = videoFadeInPercent;
        if (songVideoPlayer.HasLoadedBackgroundImage)
        {
            BackgroundImageFadeIn.Value = videoFadeInPercent;
        }

        return videoFadeInPercent;
    }

    protected virtual float UpdateAudioFadeIn()
    {
        float audioFadeInFactor = (Time.time - fadeInStartTimeInSeconds) / Math.Max(AudioFadeInDurationInSeconds, 0.001f);
        audioFadeInFactor = NumberUtils.Limit(audioFadeInFactor, 0, 1);
        float audioFadeInFactorEased = LeanTween.easeInSine(0, 1, audioFadeInFactor);

        float maxVolume = GetFinalPreviewVolume();
        songAudioPlayer.VolumeFactor = audioFadeInFactorEased * maxVolume;

        return audioFadeInFactorEased;
    }

    public virtual async void StartSongPreview(SongMeta songMeta)
    {
        if (GameObjectUtils.IsDestroyed(this)
            || !gameObject.activeInHierarchy)
        {
            return;
        }

        StopSongPreview();

        if (songMeta == currentPreviewSongMeta)
        {
            return;
        }
        currentPreviewSongMeta = songMeta;

        int delayInMillis = PreviewDelayInMillis >= 0
            ? PreviewDelayInMillis
            : settings.SongPreviewDelayInMillis;
        if (songMeta != null
            && delayInMillis > 0)
        {
            await Awaitable.WaitForSecondsAsync(delayInMillis / 1000f);
            await DoStartSongPreviewAsync(songMeta);
        }
        else
        {
            await DoStartSongPreviewAsync(songMeta);
        }
    }

    protected virtual int GetPreviewStartInMillis(SongMeta songMeta)
    {
        if (songMeta.PreviewStartInMillis > 0)
        {
            return (int)songMeta.PreviewStartInMillis;
        }

        // Fallback: find some lyrics approx. 1/3 into the song.
        Voice voice = songMeta.Voices.FirstOrDefault();
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
        int noteStartBeatInMillis = (int)SongMetaBpmUtils.BeatsToMillis(songMeta, note.StartBeat);
        return noteStartBeatInMillis;
    }

    public virtual void StopSongPreview()
    {
        if (songAudioPlayer != null)
        {
            songAudioPlayer.UnloadAudio();

            // Set volume to zero to avoid hearing the last bit of the song.
            songAudioPlayer.VolumeFactor = 0;
        }

        if (songVideoPlayer != null)
        {
            songVideoPlayer.UnloadVideo();
        }

        isFadeInStarted = false;
        stopSongPreviewEventStream.OnNext(currentPreviewSongMeta);
    }

    protected virtual async Awaitable DoStartSongPreviewAsync(SongMeta songMeta)
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
        await StartAudioPreviewAsync(songMeta, previewStartInMillis);
        await StartVideoPreviewAsync(songMeta);

        startSongPreviewEventStream.OnNext(songMeta);
    }

    protected virtual async Awaitable StartVideoPreviewAsync(SongMeta songMeta)
    {
        if (GameObjectUtils.IsDestroyed(this)
            || !gameObject.activeInHierarchy
            || songVideoPlayer == null
            || songMeta == null)
        {
            return;
        }

        // Use the audio URL as video if the WebView can handle it (e.g. a YouTube video).
        string videoUri = SongMetaUtils.GetVideoUriPreferAudioUriIfWebView(songMeta, WebViewUtils.CanHandleWebViewUrl);
        if (!SongMetaUtils.ResourceExists(songMeta, videoUri))
        {
            songVideoPlayer.UnloadVideo();
            return;
        }

        Log.Debug(() => $"StartVideoPreview '{songMeta.GetArtistDashTitle()}'");

        VideoFadeIn.Value = 0;
        BackgroundImageFadeIn.Value = 0;

        songVideoPlayer.LoadAndPlayVideoOrShowBackgroundImage(songMeta);
    }

    protected virtual async Awaitable StartAudioPreviewAsync(SongMeta songMeta, int previewStartInMillis)
    {
        if (GameObjectUtils.IsDestroyed(this)
            || !gameObject.activeInHierarchy)
        {
            return;
        }

        Log.Debug(() => $"StartAudioPreview '{songMeta.GetArtistDashTitle()}'");

        try
        {
            await songAudioPlayer.LoadAndPlayAsync(songMeta, previewStartInMillis);
            songAudioPlayer.VolumeFactor = 0;
            songAudioPlayer.PlayAudio();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to load audio '{songMeta.GetArtistDashTitle()}': {ex.Message}");

            if (ex is not DestroyedAlreadyException)
            {
                NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                    "reason", ex.Message));
            }
        }
    }

    protected virtual float GetFinalPreviewVolume()
    {
        return settings.PreviewVolumePercent / 100.0f;
    }
}
