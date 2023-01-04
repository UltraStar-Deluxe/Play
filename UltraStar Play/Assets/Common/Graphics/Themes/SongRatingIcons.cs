using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SongRatingIcons
{
    public string toneDeaf;
    public string amateur;
    public string wannabe;
    public string hopeful;
    public string risingStar;
    public string leadSinger;
    public string superstar;
    public string ultrastar;

    public string GetValueForSongRating(SongRating.ESongRating songRating)
    {
        switch (songRating)
        {
            case SongRating.ESongRating.ToneDeaf: return toneDeaf;
            case SongRating.ESongRating.Amateur: return amateur;
            case SongRating.ESongRating.Wannabe: return wannabe;
            case SongRating.ESongRating.Hopeful: return hopeful;
            case SongRating.ESongRating.RisingStar: return risingStar;
            case SongRating.ESongRating.LeadSinger: return leadSinger;
            case SongRating.ESongRating.Superstar: return superstar;
            case SongRating.ESongRating.Ultrastar: return ultrastar;
            default:
                throw new ArgumentOutOfRangeException(nameof(songRating), songRating, null);
        }
    }
}
