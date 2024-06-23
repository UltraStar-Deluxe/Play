using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public abstract class LazyLoadedSongMeta : LazyLoadedVoicesSongMeta
{
    public enum ELoadSongPhase
    {
        Pending,
        Started,
        FinishedSuccessfully,
        Failed,
    }

    [JsonIgnore]
    public virtual Action DoLoadSong { get; set; }

    [JsonIgnore]
    public ELoadSongPhase LoadSongPhase { get; private set; }

    private bool hasSetFileInfo;
    public override FileInfo FileInfo
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetFileInfo);
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
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetFileEncoding);
            return base.FileEncoding;
        }
    }

    private bool hasSetVersion;
    public override UltraStarSongFormatVersion Version
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetVersion);
            return base.Version;
        }
        set
        {
            hasSetVersion = true;
            base.Version = value;
        }
    }

    protected bool hasSetArtist;
    public override string Artist {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetArtist);
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
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetTitle);
            return base.Title;
        }
        set
        {
            hasSetTitle = true;
            base.Title = value;
        }
    }

    private bool hasSetYear;
    public override uint Year
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetYear);
            return base.Year;
        }
        set
        {
            hasSetYear = true;
            base.Year = value;
        }
    }

    private bool hasSetAudio;
    public override string Audio
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetAudio);
            return base.Audio;
        }
        set
        {
            hasSetAudio = true;
            base.Audio = value;
        }
    }

    private bool hasSetVocalsAudio;
    public override string VocalsAudio
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetVocalsAudio);
            return base.VocalsAudio;
        }

        set
        {
            hasSetVocalsAudio = true;
            base.VocalsAudio = value;
        }
    }

    private bool hasSetInstrumentalAudio;
    public override string InstrumentalAudio
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetInstrumentalAudio);
            return base.InstrumentalAudio;
        }
        set
        {
            hasSetInstrumentalAudio = true;
            base.InstrumentalAudio = value;
        }
    }

    private bool hasSetBackground;
    public override string Background
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetBackground);
            return base.Background;
        }
        set
        {
            hasSetBackground = true;
            base.Background = value;
        }
    }

    private bool hasSetCover;
    public override string Cover
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetCover);
            return base.Cover;
        }
        set
        {
            hasSetCover = true;
            base.Cover = value;
        }
    }

    private bool hasSetEdition;
    public override string Edition
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetEdition);
            return base.Edition;
        }
        set
        {
            hasSetEdition = true;
            base.Edition = value;
        }
    }

    private bool hasSetGenre;
    public override string Genre
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetGenre);
            return base.Genre;
        }
        set
        {
            hasSetGenre = true;
            base.Genre = value;
        }
    }

    private bool hasSetLanguage;
    public override string Language
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetLanguage);
            return base.Language;
        }
        set
        {
            hasSetLanguage = true;
            base.Language = value;
        }
    }

    private bool hasSetVideo;
    public override string Video
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetVideo);
            return base.Video;
        }
        set
        {
            hasSetVideo = true;
            base.Video = value;
        }
    }

    private bool hasSetBeatsPerMinute;
    public override double BeatsPerMinute
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetBeatsPerMinute);
            return base.BeatsPerMinute;
        }
        set
        {
            hasSetBeatsPerMinute = true;
            base.BeatsPerMinute = value;
        }
    }

    private bool hasSetGapInMillis;
    public override double GapInMillis
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetGapInMillis);
            return base.GapInMillis;
        }
        set
        {
            hasSetGapInMillis = true;
            base.GapInMillis = value;
        }
    }

    private bool hasSetVideoGapInMillis;
    public override double VideoGapInMillis
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetVideoGapInMillis);
            return base.VideoGapInMillis;
        }
        set
        {
            hasSetVideoGapInMillis = true;
            base.VideoGapInMillis = value;
        }
    }

    private bool hasSetPreviewStartInMillis;
    public override double PreviewStartInMillis
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetPreviewStartInMillis);
            return base.PreviewStartInMillis;
        }
        set
        {
            hasSetPreviewStartInMillis = true;
            base.PreviewStartInMillis = value;
        }
    }

    private bool hasSetPreviewEndInMillis;
    public override double PreviewEndInMillis
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetPreviewEndInMillis);
            return base.PreviewEndInMillis;
        }
        set
        {
            hasSetPreviewEndInMillis = true;
            base.PreviewEndInMillis = value;
        }
    }

    private bool hasSetStartInMillis;
    public override double StartInMillis
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetStartInMillis);
            return base.StartInMillis;
        }
        set
        {
            hasSetStartInMillis = true;
            base.StartInMillis = value;
        }
    }

    private bool hasSetEndInMillis;
    public override double EndInMillis
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetEndInMillis);
            return base.EndInMillis;
        }
        set
        {
            hasSetEndInMillis = true;
            base.EndInMillis = value;
        }
    }

    private bool hasSetMedleyStartInMillis;
    public override double MedleyStartInMillis
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetMedleyStartInMillis);
            return base.MedleyStartInMillis;
        }
        set
        {
            base.MedleyStartInMillis = value;
        }
    }

    private bool hasSetMedleyEndInMillis;
    public override double MedleyEndInMillis
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetMedleyEndInMillis);
            return base.MedleyEndInMillis;
        }
        set
        {
            hasSetMedleyEndInMillis = true;
            base.MedleyEndInMillis = value;
        }
    }

    private bool hasSetRemoteSource;
    public override string RemoteSource
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetRemoteSource);
            return base.RemoteSource;
        }
        set
        {
            hasSetRemoteSource = true;
            base.RemoteSource = value;
        }
    }

    public override string GetVoiceDisplayName(EVoiceId voiceId)
    {
        LoadSongIfNotDoneYet();
        return base.GetVoiceDisplayName(voiceId);
    }

    public override int VoiceCount
    {
        get
        {
            LoadSongIfNotDoneYet();
            return base.VoiceCount;
        }
    }

    private bool hasSetAdditionalHeaderEntries;
    public override IReadOnlyDictionary<string, string> AdditionalHeaderEntries
    {
        get
        {
            LoadSongIfNotDoneYetAndIsNotSetYet(hasSetAdditionalHeaderEntries);
            return base.AdditionalHeaderEntries;
        }
    }

    public override void SetAdditionalHeaderEntry(string key, string value)
    {
        hasSetAdditionalHeaderEntries = true;
        base.SetAdditionalHeaderEntry(key, value);
    }

    protected virtual void LoadSongIfNotDoneYetAndIsNotSetYet(bool isSet)
    {
        if (isSet)
        {
            // Has been set already
            return;
        }

        LoadSongIfNotDoneYet();
    }

    public virtual void LoadSongIfNotDoneYet()
    {
        if (LoadSongPhase is not ELoadSongPhase.Pending)
        {
            return;
        }

        try
        {
            LoadSongPhase = ELoadSongPhase.Started;
            DoLoadSong?.Invoke();
        }
        catch (Exception ex)
        {
            LoadSongPhase = ELoadSongPhase.Failed;
            Debug.LogException(ex);
            Debug.LogError($"Failed load song '{this.GetArtistDashTitle()}': {ex.Message}");
            return;
        }

        LoadSongPhase = ELoadSongPhase.FinishedSuccessfully;
    }
}
