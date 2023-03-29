using System;
using System.IO;
using UniRx;
using UnityEngine;
using UnityEngine.Experimental.Video;
using UnityEngine.Video;

public class SongAudioPlayer : MonoBehaviour
{
    // The playback position increase in milliseconds from one frame to the next to be counted as "jump".
    // An event is fired when jumping forward in the song.
    private const int MinForwardJumpOffsetInMillis = 500;

    private readonly Lazy<AudioManager> audioManagerLazy = new(() => GameObjectUtils.FindComponentWithTag<AudioManager>("AudioManager"));
    private AudioManager AudioManager => audioManagerLazy.Value;

    private readonly LazyFromComponent<AudioSource> audioPlayerLazy = new(ctx => ctx.GetComponentInChildren<AudioSource>());
    private AudioSource AudioPlayer => audioPlayerLazy.GetValue(this);
    
    private readonly LazyFromComponent<VideoPlayer> videoPlayerLazy = new(ctx => ctx.GetComponentInChildren<VideoPlayer>());
    private VideoPlayer VideoPlayer => videoPlayerLazy.GetValue(this);

    private MidiManager MidiManager => MidiManager.Instance;
    
    // The last frame in which the position in the song was calculated
    private int positionInSongInMillisFrame;

    private readonly Subject<double> playbackStoppedEventStream = new();
    public IObservable<double> PlaybackStoppedEventStream => playbackStoppedEventStream;

    private readonly Subject<double> playbackStartedEventStream = new();
    public IObservable<double> PlaybackStartedEventStream => playbackStartedEventStream;

    private readonly Subject<double> positionInSongEventStream = new();
    public IObservable<double> PositionInSongEventStream => positionInSongEventStream;

    private readonly Subject<bool> loadedEventStream = new();
    public IObservable<bool> LoadedEventStream => loadedEventStream;

    public IObservable<Pair<double>> JumpBackInSongEventStream
    {
        get
        {
            return positionInSongEventStream.Pairwise().Where(pair => pair.Previous > pair.Current);
        }
    }

    public IObservable<Pair<double>> JumpForwardInSongEventStream
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
    private double positionInSongInMillis;
    public double PositionInSongInMillis
    {
        get
        {
            if (AudioPlayer == null
                || VideoPlayer == null
                || !IsFullyLoaded)
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
                positionInSongInMillis = PositionInSongInMillisExact;
            }
            return positionInSongInMillis;
        }

        set
        {
            if (DurationOfSongInMillis <= 0)
            {
                return;
            }

            double newPositionInSongInMillis = value;
            if (newPositionInSongInMillis < 0)
            {
                newPositionInSongInMillis = 0;
            }
            else if (newPositionInSongInMillis > DurationOfSongInMillis - 1)
            {
                newPositionInSongInMillis = DurationOfSongInMillis - 1;
            }

            positionInSongInMillis = newPositionInSongInMillis;

            float newTimeInSeconds = (float)(value / 1000.0);
            
            if (HasAudio)
            {
                AudioPlayer.time = newTimeInSeconds;
            }
            else if (HasVideo)
            {
                 VideoPlayer.time = newTimeInSeconds;
            }

            positionInSongEventStream.OnNext(positionInSongInMillis);
        }
    }

    public double PositionInSongInSeconds => positionInSongInMillis / 1000.0;

    /**
     * Returns the exact position in the song based on current sample position.
     * Note that this changes concurrently,
     * such that it can return different values when called multiple times in the same frame.
     */
    public double PositionInSongInMillisExact
    {
        get
        {
            if (HasAudio)
            {
                return ((double)AudioPlayer.timeSamples / (double)AudioPlayer.clip.frequency) * 1000.0;
            }
            else if (HasVideo)
            {
                return VideoPlayer.time * 1000.0;
            }

            return 0;
        }
    }

    public double DurationOfSongInMillis { get; private set; }
    public double DurationOfSongInSeconds => DurationOfSongInMillis / 1000.0;

    /**
     * Position in the song from 0 (start of song) to 1 (end of song).
     */
    public double PositionInSongInPercent
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
            return !isPaused
                   && (AudioPlayer.isPlaying || VideoPlayer.isPlaying);
        }
    }

    public bool IsPartiallyLoaded => HasAudio || HasVideo;
    public bool IsFullyLoaded => (HasAudio && AudioPlayer.clip.length > 0) 
                                 || (HasVideo && VideoPlayer.length > 0);
    
    private SongMeta SongMeta { get; set; }

    public float VolumeFactor
    {
        get => AudioPlayer.volume;
        set => AudioPlayer.volume = value;
    }

    public float Pitch
    {
        get => AudioPlayer.pitch;
        set => AudioPlayer.pitch = value;
    }

    public float PlaybackSpeed
    {
        get
        {
            return AudioPlayer.pitch;
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
            AudioPlayer.pitch = newPlaybackSpeed;

            // A Pitch Shifter effect on an AudioMixerGroup can be used to compensate the pitch change of the AudioPlayer,
            // such that only the change of the tempo remains.
            // See here for details: https://answers.unity.com/questions/25139/how-i-can-change-the-speed-of-a-song-or-sound.html
            // See here for how the pitch value of the Pitch Shifter effect is made available for scripting: https://learn.unity.com/tutorial/audio-mixing#5c7f8528edbc2a002053b506
            AudioPlayer.outputAudioMixerGroup.audioMixer.SetFloat("PitchShifter.Pitch", 1 + (1 - newPlaybackSpeed));
        }
    }

    private bool HasAudio => AudioPlayer.clip != null;
    private bool HasVideo => !VideoPlayer.url.IsNullOrEmpty();

    private bool isPaused;
    
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

        string audioUri = SongMetaUtils.GetAudioUri(songMeta);
        if (!SongMetaUtils.AudioResourceExists(songMeta))
        {
            Debug.Log($"Audio file resource does not exist {songMeta.Mp3}");
            return;
        }

        SongMeta = songMeta;
        
        ResetAudioAndVideo();
        
        string fileExtension = Path.GetExtension(audioUri);
        if (ApplicationUtils.IsSupportedVideoFormat(fileExtension))
        {
            // Load video file
            LoadAsVideo(audioUri);
        }
        else if (ApplicationUtils.IsSupportedMidiFormat(fileExtension))
        {
            LoadMidiAsAudio(audioUri);
        }
        else
        {
            // Load audio file
            LoadAsAudio(audioUri);
        }
    }

    private void ResetAudioAndVideo()
    {
        VideoPlayer.Stop();
        VideoPlayer.url = "";

        AudioPlayer.Stop();
        AudioPlayer.clip = null;
        
        DurationOfSongInMillis = 0;
    }
    
    private void LoadMidiAsAudio(string uri)
    {
        AudioClip audioClip = MidiManager.CreateAudioClip(uri);
        if (audioClip == null)
        {
            Debug.LogError($"Failed to load audio clip from MIDI file {uri}");
            AudioPlayer.Stop();
            return;
        }
        
        AudioPlayer.clip = audioClip;
        DurationOfSongInMillis = 1000.0 * audioClip.samples / audioClip.frequency;
        loadedEventStream.OnNext(true);
    }
    
    private void LoadAsAudio(string audioUri)
    {
        AudioClip audioClip = AudioManager.LoadAudioClipFromUri(audioUri);
        if (audioClip == null)
        {
            Debug.LogError($"Failed to load audio clip from {audioUri}");
            AudioPlayer.Stop();
            return;
        }

        AudioPlayer.clip = audioClip;
        DurationOfSongInMillis = 1000.0 * audioClip.samples / audioClip.frequency;
        loadedEventStream.OnNext(true);
    }

    private void LoadAsVideo(string audioUri)
    {
        VideoPlayer.url = audioUri;
        if (VideoPlayer.url.IsNullOrEmpty())
        {
            Debug.LogError($"Failed to load video from {audioUri}");
            VideoPlayer.Stop();
            return;
        }
        
        // Play the audio of the video player through the AudioSource.
        VideoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        for (int trackIndex = 0; trackIndex < VideoPlayer.audioTrackCount; trackIndex++)
        {
            VideoPlayer.SetTargetAudioSource(0, AudioPlayer);
        }

        // Must play the video to trigger loading.
        VideoPlayer.Play();

        // The video is loaded asynchronously. The length property of the VideoPlayer indicates whether it has been loaded.
        StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
            () => VideoPlayer.length > 0, () =>
            {
                DurationOfSongInMillis = 1000.0 * VideoPlayer.length;
                PauseAudio();
                loadedEventStream.OnNext(true);
            }));
    }

    public void ReloadAudio()
    {
        Init(SongMeta);
    }

    public void PauseAudio()
    {
        if (!IsPlaying)
        {
            return;
        }

        AudioPlayer.Pause();
        VideoPlayer.Pause();
        isPaused = true;
        playbackStoppedEventStream.OnNext(PositionInSongInMillis);
    }

    public void PlayAudio()
    {
        if (IsPlaying
            || !IsPartiallyLoaded)
        {
            return;
        }

        if (HasAudio)
        {
            AudioPlayer.Play();
        }
        else if (HasVideo)
        {
            VideoPlayer.Play();
        }
        isPaused = false;
        playbackStartedEventStream.OnNext(PositionInSongInMillis);
    }

    public double GetCurrentBeat(bool allowNegativeResult)
    {
        if (!IsFullyLoaded)
        {
            return 0;
        }

        double millisInSong = PositionInSongInMillis;
        double result = BpmUtils.MillisecondInSongToBeat(SongMeta, millisInSong);
        if (result < 0
            && !allowNegativeResult)
        {
            result = 0;
        }
        return result;
    }
}
