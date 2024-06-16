using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneAlternativeAudioPlayer : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public AudioSource instrumentalAudioSource;

    [InjectedInInspector]
    public AudioSource vocalsAudioSource;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private AudioSeparationManager audioSeparationManager;

    [Inject]
    private Settings settings;

    [Inject]
    private SingSceneAudioFadeInControl audioFadeInControl;

    [Inject]
    private SingSceneControl singSceneControl;

    private bool hasLoadedInstrumentalAndVocalsAudio;

    private bool isInitialized;

    private void Start()
    {
        audioSeparationManager.AudioSeparationFinishedEventStream
            .Subscribe(evt =>
            {
                if (evt.SongMeta == songMeta)
                {
                    Init();
                }
            })
            .AddTo(gameObject);

        singSceneControl.ModifiedVolumePercent
            .Subscribe(_ => UpdateAudioSources())
            .AddTo(gameObject);
        audioFadeInControl.FadeInVolumePercent
            .Subscribe(_ => UpdateAudioSources())
            .AddTo(gameObject);

        settings.ObserveEveryValueChanged(it => it.MusicVolumePercent)
            .Subscribe(_ => UpdateAudioSources())
            .AddTo(gameObject);
        settings.ObserveEveryValueChanged(it => it.VocalsAudioVolumePercent)
            .Subscribe(_ => UpdateAudioSources())
            .AddTo(gameObject);

        songAudioPlayer.PlaybackStartedEventStream
            .Subscribe(_ =>
            {
                PlayInstrumentalAndVocalsAudio();
                SyncAudioPosition();
            })
            .AddTo(gameObject);
        songAudioPlayer.PlaybackStoppedEventStream
            .Subscribe(_ => PauseInstrumentalAndVocalsAudio())
            .AddTo(gameObject);
        songAudioPlayer.JumpForwardEventStream
            .Subscribe(_ => SyncAudioPosition())
            .AddTo(gameObject);
        songAudioPlayer.JumpBackEventStream
            .Subscribe(_ => SyncAudioPosition())
            .AddTo(gameObject);

        Init();
    }

    private void Init()
    {
        if (isInitialized
            || !CanPlayInstrumentalAndVocalsAudio(out string errorMessage))
        {
            return;
        }

        UpdateAudioSources();

        isInitialized = true;
    }

    private void PauseInstrumentalAndVocalsAudio()
    {
        if (instrumentalAudioSource.clip != null)
        {
            instrumentalAudioSource.Pause();
        }

        if (vocalsAudioSource.clip != null)
        {
            vocalsAudioSource.Pause();
        }
    }

    private void PlayInstrumentalAndVocalsAudio()
    {
        bool shouldSync = false;

        if (instrumentalAudioSource.clip != null
            && !instrumentalAudioSource.isPlaying)
        {
            instrumentalAudioSource.Play();
            shouldSync = true;
        }

        if (vocalsAudioSource.clip != null
            && !vocalsAudioSource.isPlaying)
        {
            vocalsAudioSource.Play();
            shouldSync = true;
        }

        if (shouldSync)
        {
            SyncAudioPosition();
        }
    }

    private void SyncAudioPosition()
    {
        float songAudioPlayerTimeInSeconds = (float)songAudioPlayer.PositionInSeconds;
        if (instrumentalAudioSource.clip != null
            && !instrumentalAudioSource.isPlaying)
        {
            instrumentalAudioSource.time = songAudioPlayerTimeInSeconds;
        }

        if (vocalsAudioSource.clip != null
            && !vocalsAudioSource.isPlaying)
        {
            vocalsAudioSource.time = songAudioPlayerTimeInSeconds;
        }
    }

    public void UpdateAudioSources()
    {
        if (settings.VocalsAudioVolumePercent >= 100
            || !CanPlayInstrumentalAndVocalsAudio(out string errorMessage))
        {
            UseOriginalSongAudio();
        }
        else
        {
            UseInstrumentalAndVocalsAudio();
        }
    }

    private void UseOriginalSongAudio()
    {
        songAudioPlayer.VolumeFactor = NumberUtils.PercentToFactor(settings.MusicVolumePercent)
                                       * NumberUtils.PercentToFactor(singSceneControl.ModifiedVolumePercent.Value)
                                       * NumberUtils.PercentToFactor(audioFadeInControl.FadeInVolumePercent.Value);
        vocalsAudioSource.volume = 0;
        instrumentalAudioSource.volume = 0;

        PauseInstrumentalAndVocalsAudio();
    }

    private void UseInstrumentalAndVocalsAudio()
    {
        if (!hasLoadedInstrumentalAndVocalsAudio)
        {
            hasLoadedInstrumentalAndVocalsAudio = true;
            instrumentalAudioSource.clip = AudioManager.LoadAudioClipFromUriImmediately(SongMetaUtils.GetInstrumentalAudioUri(songMeta), true);
            vocalsAudioSource.clip = AudioManager.LoadAudioClipFromUriImmediately(SongMetaUtils.GetVocalsAudioUri(songMeta), true);
        }

        songAudioPlayer.VolumeFactor = 0;
        instrumentalAudioSource.volume = NumberUtils.PercentToFactor(settings.MusicVolumePercent)
                                         * NumberUtils.PercentToFactor(singSceneControl.ModifiedVolumePercent.Value)
                                         * NumberUtils.PercentToFactor(audioFadeInControl.FadeInVolumePercent.Value);
        vocalsAudioSource.volume = NumberUtils.PercentToFactor(settings.MusicVolumePercent)
                                   * NumberUtils.PercentToFactor(settings.VocalsAudioVolumePercent)
                                   * NumberUtils.PercentToFactor(singSceneControl.ModifiedVolumePercent.Value)
                                   * NumberUtils.PercentToFactor(audioFadeInControl.FadeInVolumePercent.Value);

        if (songAudioPlayer.IsPlaying)
        {
            PlayInstrumentalAndVocalsAudio();
        }
    }

    private bool CanPlayInstrumentalAndVocalsAudio(out string errorMessage)
    {
        if (songMeta.VocalsAudio.IsNullOrEmpty())
        {
            errorMessage = "No vocals audio found. Split the audio first.";
            return false;
        }
        if (!SongMetaUtils.VocalsAudioResourceExists(songMeta))
        {
            errorMessage = $"Resource does not exist: {songMeta.VocalsAudio}";
            return false;
        }

        if (songMeta.InstrumentalAudio.IsNullOrEmpty())
        {
            errorMessage = "No instrumental audio found. Split the audio first.";
            return false;
        }
        if (!SongMetaUtils.InstrumentalAudioResourceExists(songMeta))
        {
            errorMessage = $"File does not exist: {songMeta.VocalsAudio}";
            return false;
        }

        errorMessage = "";
        return true;
    }
}
