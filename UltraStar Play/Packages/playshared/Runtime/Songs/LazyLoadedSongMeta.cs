using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
public class LazyLoadedSongMeta : LazyLoadedVoicesSongMeta
{
    private enum ELoadSongPhase
    {
        Pending,
        Started,
        FinishedSuccessfully,
        Failed,
    }

    public virtual Action OnLoadSong { get; set; }

    public bool HasFailedToLoadSong => loadSongPhase is ELoadSongPhase.Failed;
    private ELoadSongPhase loadSongPhase;

    private bool hasSetFileInfo;
    public override FileInfo FileInfo
    {
        get
        {
            if (!hasSetFileInfo)
            {
                LoadSongIfNotDoneYet();
            }
            return base.FileInfo;
        }
    }

    public override void SetFileInfo(FileInfo filePath, Encoding encoding = null)
    {
        hasSetFileInfo = true;
        hasSetFileEncoding = true;
        base.SetFileInfo(filePath, encoding);
    }

    private bool hasSetFileEncoding;
    public override Encoding FileEncoding
    {
        get
        {
            if (!hasSetFileEncoding)
            {
                LoadSongIfNotDoneYet();
            }
            return base.FileEncoding;
        }
    }

    protected bool hasSetArtist;
    public override string Artist {
        get
        {
            if (!hasSetArtist)
            {
                LoadSongIfNotDoneYet();
            }
            return base.Artist;
        }
        set
        {
            hasSetArtist = true;
            base.Artist = value;
        }
    }

    protected bool hasSetTitle;
    public override string Title
    {
        get
        {
            if (!hasSetTitle)
            {
                LoadSongIfNotDoneYet();
            }
            return base.Title;
        }
        set
        {
            hasSetTitle = true;
            base.Title = value;
        }
    }

    public override uint Year
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.Year;
        }
    }

    public override string Audio
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.Audio;
        }
    }

    public override string VocalsAudio
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.VocalsAudio;
        }
    }

    public override string InstrumentalAudio
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.InstrumentalAudio;
        }
    }

    public override string Website
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.Website;
        }
    }

    public override string Background
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.Background;
        }
    }

    public override string Cover
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.Cover;
        }
    }

    public override string Edition
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.Edition;
        }
    }

    public override string Genre
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.Genre;
        }
    }

    public override string Language
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.Language;
        }
    }

    public override string Video
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.Video;
        }
    }

    public override double BeatsPerMinute
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.BeatsPerMinute;
        }
    }

    public override double GapInMillis
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.GapInMillis;
        }
    }

    public override double VideoGapInMillis
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.VideoGapInMillis;
        }
    }

    public override double PreviewStartInMillis
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.PreviewStartInMillis;
        }
    }

    public override double PreviewEndInMillis
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.PreviewEndInMillis;
        }
    }

    public override double StartInMillis
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.StartInMillis;
        }
    }

    public override double EndInMillis
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.EndInMillis;
        }
    }

    public override double MedleyStartInMillis
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.MedleyStartInMillis;
        }
    }

    public override double MedleyEndInMillis
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.MedleyEndInMillis;
        }
    }

    public override IReadOnlyCollection<Voice> Voices
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.Voices;
        }
    }

    public override int VoiceCount
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.VoiceCount;
        }
    }

    public override string RemoteSource
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.RemoteSource;
        }
    }

    public override IReadOnlyDictionary<string, string> AdditionalHeaderEntries
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.AdditionalHeaderEntries;
        }
    }

    protected virtual void LoadSongIfNotDoneYet()
    {
        if (loadSongPhase is not ELoadSongPhase.Pending)
        {
            return;
        }

        try
        {
            loadSongPhase = ELoadSongPhase.Started;
            OnLoadSong?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to lazy load song '{SongMetaUtils.GetArtistDashTitle(this)}': {ex.Message}");
            loadSongPhase = ELoadSongPhase.Failed;
            return;
        }

        loadSongPhase = ELoadSongPhase.FinishedSuccessfully;
    }
}
