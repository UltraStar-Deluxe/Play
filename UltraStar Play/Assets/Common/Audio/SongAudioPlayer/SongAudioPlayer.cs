using System;
using UniInject;
using UniRx;
using UnityEngine;

public class SongAudioPlayer : MonoBehaviour
{
    // The playback position increase in milliseconds from one frame to the next to be counted as "jump".
    // An event is fired when jumping forward in the song.
    private const int MinForwardJumpOffsetInMillis = 500;

    [InjectedInInspector]
    public AudioSource audioPlayer;

    // The last frame in which the position in the song was calculated
    private int positionInSongInMillisFrame;

    private readonly Subject<float> playbackStoppedEventStream = new Subject<float>();
    public IObservable<float> PlaybackStoppedEventStream => playbackStoppedEventStream;

    private readonly Subject<float> playbackStartedEventStream = new Subject<float>();
    public IObservable<float> PlaybackStartedEventStream => playbackStartedEventStream;

    private readonly Subject<float> positionInSongEventStream = new Subject<float>();
    public IObservable<float> PositionInSongEventStream => positionInSongEventStream;

    private readonly Subject<AudioClip> audioClipLoadedEventStream = new Subject<AudioClip>();
    public IObservable<AudioClip> AudioClipLoadedEventStream => audioClipLoadedEventStream;

    public IObservable<Pair<float>> JumpBackInSongEventStream
    {
        get
        {
            return positionInSongEventStream.Pairwise().Where(pair => pair.Previous > pair.Current);
        }
    }

    public IObservable<Pair<float>> JumpForwardInSongEventStream
    {
        get
        {
            // The position will increase in normal playback. A big increase however, can always be considered as "jump".
            // Furthermore, when not currently playing, then every forward change can be considered as "jump".
            return positionInSongEventStream.Pairwise().Where(pair =>
            {
                return (pair.Previous + MinForwardJumpOffsetInMillis) < pair.Current
                    || (!IsPlaying && pair.Previous < pair.Current);
            });
        }
    }

    // The current position in the song in milliseconds.
    private float positionInSongInMillis;
    public float PositionInSongInMillis
    {
        get
        {
            if (audioPlayer == null || audioPlayer.clip == null)
            {
                return 0;
            }
            // The samples of an AudioClip change concurrently,
            // even when they are queried in the same frame (e.g. Update() of different scripts).
            // For a given frame, the position in the song should be the same for all scripts,
            // which is why the value is only updated once per frame.
            if (positionInSongInMillisFrame != Time.frameCount)
            {
                positionInSongInMillisFrame = Time.frameCount;
                positionInSongInMillis = 1000.0f * (float)audioPlayer.timeSamples / (float)audioPlayer.clip.frequency;
            }
            return positionInSongInMillis;
        }

        set
        {
            if (DurationOfSongInMillis <= 0)
            {
                return;
            }

            float newPositionInSongInMillis = value;
            if (newPositionInSongInMillis < 0)
            {
                newPositionInSongInMillis = 0;
            }
            else if (newPositionInSongInMillis > DurationOfSongInMillis - 1)
            {
                newPositionInSongInMillis = DurationOfSongInMillis - 1;
            }

            positionInSongInMillis = newPositionInSongInMillis;
            int newTimeSamples = (int)(audioPlayer.clip.frequency * positionInSongInMillis / 1000.0);
            audioPlayer.timeSamples = newTimeSamples;

            positionInSongEventStream.OnNext(positionInSongInMillis);
        }
    }

    public float DurationOfSongInMillis { get; private set; }

    public float PositionInSongInPercent
    {
        get
        {
            if (DurationOfSongInMillis <= 0)
            {
                return 0;
            }

            return PositionInSongInMillis / DurationOfSongInMillis;
        }
    }

    public bool IsPlaying
    {
        get
        {
            return audioPlayer.isPlaying;
        }
    }

    public AudioClip AudioClip
    {
        get
        {
            return audioPlayer.clip;
        }
    }

    public bool HasAudioClip
    {
        get
        {
            return audioPlayer.clip != null;
        }
    }

    private SongMeta SongMeta { get; set; }

    public float PlaybackSpeed
    {
        get
        {
            return audioPlayer.pitch;
        }
        set
        {
            // Playback speed cannot be set randomly. Allowed (and useful) is a range of 0.5 to 1.5.
            float newPlaybackSpeed = value;
            if (newPlaybackSpeed < 0.5f)
            {
                newPlaybackSpeed = 0.5f;
            }
            else if (newPlaybackSpeed > 1.5f)
            {
                newPlaybackSpeed = 1.5f;
            }

            // Setting the pitch of an AudioPlayer will change tempo and pitch.
            audioPlayer.pitch = newPlaybackSpeed;

            // A Pitch Shifter effect on an AudioMixerGroup can be used to compensate the pitch change of the AudioPlayer,
            // such that only the change of the tempo remains.
            // See here for details: https://answers.unity.com/questions/25139/how-i-can-change-the-speed-of-a-song-or-sound.html
            // See here for how the pitch value of the Pitch Shifter effect is made available for scripting: https://learn.unity.com/tutorial/audio-mixing#5c7f8528edbc2a002053b506
            audioPlayer.outputAudioMixerGroup.audioMixer.SetFloat("PitchShifter.Pitch", 1 + (1 - newPlaybackSpeed));
        }
    }

    private void Update()
    {
        if (IsPlaying)
        {
            positionInSongEventStream.OnNext(PositionInSongInMillis);
        }
    }

    public void Init(SongMeta songMeta)
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        this.SongMeta = songMeta;
        string audioUri = SongMetaUtils.GetAudioUri(songMeta);
        AudioClip audioClip = AudioManager.Instance.LoadAudioClipFromUri(audioUri);
        if (audioClip != null)
        {
            audioPlayer.clip = audioClip;
            DurationOfSongInMillis = 1000.0f * audioClip.samples / audioClip.frequency;
            audioClipLoadedEventStream.OnNext(audioClip);
        }
        else
        {
            audioPlayer.clip = null;
            DurationOfSongInMillis = 0;
        }
    }

    public void ReloadAudio()
    {
        Init(SongMeta);
    }

    public void PauseAudio()
    {
        if (audioPlayer.isPlaying)
        {
            audioPlayer.Pause();
            playbackStoppedEventStream.OnNext(PositionInSongInMillis);
        }
    }

    public void PlayAudio()
    {
        if (HasAudioClip && !audioPlayer.isPlaying)
        {
            audioPlayer.Play();
            playbackStartedEventStream.OnNext(PositionInSongInMillis);
        }
    }

    public float GetCurrentBeat(bool allowNegativeResult)
    {
        if (audioPlayer.clip == null)
        {
            return 0;
        }

        float millisInSong = PositionInSongInMillis;
        float result = BpmUtils.MillisecondInSongToBeat(SongMeta, millisInSong);
        if (result < 0
            && !allowNegativeResult)
        {
            result = 0;
        }
        return result;
    }
}
