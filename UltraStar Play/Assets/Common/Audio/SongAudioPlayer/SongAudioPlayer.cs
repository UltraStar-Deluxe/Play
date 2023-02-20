using System;
using System.IO;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Video;

public class SongAudioPlayer : MonoBehaviour, INeedInjection
{
    // The playback position increase in milliseconds from one frame to the next to be counted as "jump".
    // An event is fired when jumping forward in the song.
    private const int MinForwardJumpOffsetInMillis = 500;
    
    [Inject]
    private AudioManager audioManager;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private AudioSource audioPlayer;

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private VideoPlayer videoPlayer;
    
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
            if (audioPlayer == null
                || videoPlayer == null
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
                audioPlayer.time = newTimeInSeconds;
            }
            else if (HasVideo)
            {
                videoPlayer.time = newTimeInSeconds;
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
                return ((double)audioPlayer.timeSamples / (double)audioPlayer.clip.frequency) * 1000.0;
            }
            else if (HasVideo)
            {
                return videoPlayer.time * 1000.0;
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
                   && (audioPlayer.isPlaying || videoPlayer.isPlaying);
        }
    }

    public bool IsPartiallyLoaded => HasAudio || HasVideo;
    public bool IsFullyLoaded => (HasAudio && audioPlayer.clip.length > 0) 
                                 || (HasVideo && videoPlayer.length > 0);
    
    private SongMeta SongMeta { get; set; }

    public float VolumeFactor
    {
        get => audioPlayer.volume;
        set => audioPlayer.volume = value;
    }

    public float Pitch
    {
        get => audioPlayer.pitch;
        set => audioPlayer.pitch = value;
    }

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

    private bool HasAudio => audioPlayer.clip != null;
    private bool HasVideo => !videoPlayer.url.IsNullOrEmpty();

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
        else
        {
            // Load audio file
            LoadAsAudio(audioUri);
        }
    }

    private void ResetAudioAndVideo()
    {
        videoPlayer.Stop();
        videoPlayer.url = "";

        audioPlayer.Stop();
        audioPlayer.clip = null;
        
        DurationOfSongInMillis = 0;
    }
    
    private void LoadAsAudio(string audioUri)
    {
        AudioClip audioClip = audioManager.LoadAudioClipFromUri(audioUri);
        if (audioClip == null)
        {
            Debug.LogError($"Failed to load audio clip from {audioUri}");
            audioPlayer.Stop();
            return;
        }

        audioPlayer.clip = audioClip;
        DurationOfSongInMillis = 1000.0 * audioClip.samples / audioClip.frequency;
        loadedEventStream.OnNext(true);
    }

    private void LoadAsVideo(string audioUri)
    {
        videoPlayer.url = audioUri;
        if (videoPlayer.url.IsNullOrEmpty())
        {
            Debug.LogError($"Failed to load video from {audioUri}");
            videoPlayer.Stop();
            return;
        }
        
        // Play the audio of the video player through the AudioSource.
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        for (int trackIndex = 0; trackIndex < videoPlayer.audioTrackCount; trackIndex++)
        {
            videoPlayer.SetTargetAudioSource(0, audioPlayer);
        }

        // The video is loaded asynchronously. The length property of the VideoPlayer indicates whether it has been loaded.
        StartCoroutine(CoroutineUtils.ExecuteWhenConditionIsTrue(
            () => videoPlayer.length > 0, () =>
            {
                DurationOfSongInMillis = 1000.0 * videoPlayer.length;
                loadedEventStream.OnNext(true);
                Debug.Log($"Loaded as video: duration: {DurationOfSongInMillis}, position: {PositionInSongInMillis}");
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

        audioPlayer.Pause();
        videoPlayer.Pause();
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
            audioPlayer.Play();
        }
        else if (HasVideo)
        {
            videoPlayer.Play();
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
