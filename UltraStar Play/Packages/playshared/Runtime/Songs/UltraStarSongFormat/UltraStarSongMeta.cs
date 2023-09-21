using System;
using System.Collections.Generic;
using UnityEngine;

public class UltraStarSongMeta : LazyLoadedVoicesSongMeta
{
    /**
     * The "bars-per-minute" in four-four-time (i.e. (beats-per-minute / 4)) of the song.
     * Example: a BPM value of 60 in a txt file would define a beat every 0.25 seconds (60*4=240 beats-per-minute).
     */
    public double TxtFileBpm {
        get
        {
            return BeatsPerMinute / 4.0;
        }
        set
        {
            BeatsPerMinute = value * 4.0;
        }
    }

    public double TxtFilePreviewStartInSeconds
    {
        get
        {
            return PreviewStartInMillis / 1000.0;
        }
        set
        {
            PreviewStartInMillis = value * 1000.0;
        }
    }

    public double TxtFilePreviewEndInSeconds
    {
        get
        {
            return PreviewEndInMillis / 1000.0;
        }
        set
        {
            PreviewEndInMillis = value * 1000.0;
        }
    }

    public double TxtFileStartInSeconds
    {
        get
        {
            return StartInMillis / 1000.0;
        }
        set
        {
            StartInMillis = value * 1000.0;
        }
    }

    public double TxtFileEndInMillis
    {
        get
        {
            return EndInMillis;
        }
        set
        {
            EndInMillis = value;
        }
    }

    public double TxtFileVideoGapInSeconds
    {
        get
        {
            return VideoGapInMillis / 1000.0;
        }
        set
        {
            VideoGapInMillis = value * 1000.0;
        }
    }

    public double TxtFileMedleyStartBeat
    {
        get
        {
            return SongMetaBpmUtils.MillisToBeats(this, MedleyStartInMillis);
        }
        set
        {
            MedleyStartInMillis = SongMetaBpmUtils.BeatsToMillis(this, value);
        }
    }

    public double TxtFileMedleyEndBeat
    {
        get
        {
            return (int)SongMetaBpmUtils.MillisToBeats(this, MedleyEndInMillis);
        }
        set
        {
            MedleyEndInMillis = (int)SongMetaBpmUtils.BeatsToMillis(this, value);
        }
    }

    public override int VoiceCount => !voiceIdToDisplayName.IsNullOrEmpty()
        ? voiceIdToDisplayName.Count
        : Voices.Count;

    public UltraStarSongMeta(SongMeta other)
    {
        CopyValues(other);
        OnLoadVoices = DoLoadVoices;
    }

    public UltraStarSongMeta(
        string artist,
        string title,
        float txtFileBpm,
        string audioFile,
        Dictionary<EVoiceId, string> voiceIdToDisplayName)
    {
        Artist = artist ?? throw new ArgumentNullException(nameof(artist));
        TxtFileBpm = txtFileBpm;
        Audio = audioFile ?? throw new ArgumentNullException(nameof(audioFile));
        Title = title ?? throw new ArgumentNullException(nameof(title));

        if (voiceIdToDisplayName == null)
        {
            throw new ArgumentNullException(nameof(voiceIdToDisplayName));
        }
        this.voiceIdToDisplayName.AddRange(voiceIdToDisplayName);

        OnLoadVoices = DoLoadVoices;
    }

    private void DoLoadVoices()
    {
        if (FileInfo == null
            || !FileInfo.Exists)
        {
            Debug.LogError($"Failed to lazy load voices of {GetType().Name} '{SongMetaUtils.GetArtistDashTitle(this)}' because no file reference is set." +
                           $"Try adding the voices manually or set another {nameof(OnLoadVoices)} callback.");
            return;
        }

        bool.TryParse(GetAdditionalHeaderEntry("relative"), out bool isRelativeSongFormat);

        List<Voice> voices = UltraStarSongVoicesParser.ParseFile(
            FileInfo.FullName,
            FileEncoding,
            isRelativeSongFormat,
            false);
        voices.ForEach(voice => AddVoice(voice));
    }
}
