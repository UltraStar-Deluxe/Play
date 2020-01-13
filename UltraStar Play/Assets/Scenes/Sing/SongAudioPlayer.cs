using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UniInject;
using UnityEngine;
using UniRx;

public class SongAudioPlayer : MonoBehaviour
{
    [InjectedInInspector]
    public AudioSource audioPlayer;

    // The last frame in which the position in the song was calculated
    private int positionInSongInMillisFrame;

    private readonly Subject<double> positionInSongEventStream = new Subject<double>();
    public ISubject<double> PositionInSongEventStream
    {
        get
        {
            return positionInSongEventStream;
        }
    }

    // The current position in the song in milliseconds.
    private double positionInSongInMillis;
    public double PositionInSongInMillis
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
                positionInSongInMillis = 1000.0f * (double)audioPlayer.timeSamples / (double)audioPlayer.clip.frequency;
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
            int newTimeSamples = (int)((positionInSongInMillis / 1000.0) * audioPlayer.clip.frequency);
            audioPlayer.timeSamples = newTimeSamples;

            positionInSongEventStream.OnNext(positionInSongInMillis);
        }
    }

    public double DurationOfSongInMillis { get; private set; }

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

    public double CurrentBeat
    {
        get
        {
            if (audioPlayer.clip == null)
            {
                return 0;
            }
            else
            {
                double millisInSong = PositionInSongInMillis;
                double result = BpmUtils.MillisecondInSongToBeat(SongMeta, millisInSong);
                if (result < 0)
                {
                    result = 0;
                }
                return result;
            }
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

    void Update()
    {
        if (IsPlaying)
        {
            positionInSongEventStream.OnNext(PositionInSongInMillis);
        }
    }

    public void Init(SongMeta songMeta)
    {
        this.SongMeta = songMeta;

        LoadAudio();
    }

    private void LoadAudio()
    {
        string songPath = SongMeta.Directory + Path.DirectorySeparatorChar + SongMeta.Mp3;
        AudioClip audioClip = AudioManager.GetAudioClip(songPath);
        if (audioClip != null)
        {
            audioPlayer.clip = audioClip;
            DurationOfSongInMillis = 1000.0 * audioClip.samples / audioClip.frequency;
        }
        else
        {
            DurationOfSongInMillis = 0;
        }
    }

    public void PauseAudio()
    {
        audioPlayer.Pause();
    }

    public void PlayAudio()
    {
        audioPlayer.Play();
    }
}
