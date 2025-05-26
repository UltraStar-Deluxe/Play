using System;

[Serializable]
public class SongRatingIconsJson
{
    public string toneDeaf;
    public string amateur;
    public string wannabe;
    public string hopeful;
    public string risingStar;
    public string leadSinger;
    public string superstar;
    public string ultrastar;

    public string GetValueForSongRating(ESongRating songRating)
    {
        switch (songRating)
        {
            case ESongRating.ToneDeaf: return toneDeaf;
            case ESongRating.Amateur: return amateur;
            case ESongRating.Wannabe: return wannabe;
            case ESongRating.Hopeful: return hopeful;
            case ESongRating.RisingStar: return risingStar;
            case ESongRating.LeadSinger: return leadSinger;
            case ESongRating.Superstar: return superstar;
            case ESongRating.Ultrastar: return ultrastar;
            default:
                throw new ArgumentOutOfRangeException(nameof(songRating), songRating, null);
        }
    }
}
