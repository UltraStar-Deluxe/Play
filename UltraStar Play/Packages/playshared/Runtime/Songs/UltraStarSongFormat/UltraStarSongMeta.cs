using System;
using System.Collections.Generic;

public class UltraStarSongMeta : LazyLoadedSongMeta
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

    public bool IsTxtFileRelative
    {
        get
        {
            if (bool.TryParse(GetAdditionalHeaderEntry("relative"), out bool relative))
            {
                return relative;
            }
            return false;
        }
    }

    public override int VoiceCount => !voiceIdToDisplayName.IsNullOrEmpty()
        ? voiceIdToDisplayName.Count
        : Voices.Count;

    public UltraStarSongMeta()
    {
    }

    public UltraStarSongMeta(SongMeta other)
    {
        CopyValues(other);
    }

    public override void CopyValues(SongMeta other)
    {
        base.CopyValues(other);
        if (other is UltraStarSongMeta otherUltraStarSongMeta)
        {
            Version = otherUltraStarSongMeta.Version;
        }
        else
        {
            Version = UltraStarSongFormatVersion.unknown;
        }
    }

    public UltraStarSongMeta(
        string artist,
        string title,
        double txtFileBpm,
        string audioFile,
        Dictionary<EVoiceId, string> voiceIdToDisplayName)
    : this(artist, title, txtFileBpm, audioFile, voiceIdToDisplayName, UltraStarSongFormatVersion.unknown)
    {
    }

    public UltraStarSongMeta(
        string artist,
        string title,
        double txtFileBpm,
        string audioFile,
        Dictionary<EVoiceId, string> voiceIdToDisplayName,
        UltraStarSongFormatVersion version)
    {
        Artist = artist.OrIfNull("");
        TxtFileBpm = txtFileBpm;
        Audio = audioFile.OrIfNull("");
        Title = title.OrIfNull("");
        Version = version;

        if (voiceIdToDisplayName == null)
        {
            throw new ArgumentNullException(nameof(voiceIdToDisplayName));
        }
        this.voiceIdToDisplayName.AddRange(voiceIdToDisplayName);
    }
}
