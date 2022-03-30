using System.Collections.Generic;
using ProTrans;
using UnityEngine;

public class SongRating
{
    public static readonly SongRating ToneDeaf = new(ESongRating.ToneDeaf, 0, R.Messages.rating_song_toneDeaf);
    public static readonly SongRating Amateur = new(ESongRating.Amateur, 2000, R.Messages.rating_song_amateur);
    public static readonly SongRating Wannabe = new(ESongRating.Wannabe, 4000, R.Messages.rating_song_wannabe);
    public static readonly SongRating Hopeful = new(ESongRating.Hopeful, 5000, R.Messages.rating_song_hopeful);
    public static readonly SongRating RisingStar = new(ESongRating.RisingStar, 6000, R.Messages.rating_song_risingStar);
    public static readonly SongRating LeadSinger = new(ESongRating.LeadSinger, 7500, R.Messages.rating_song_leadSinger);
    public static readonly SongRating Superstar = new(ESongRating.Superstar, 8500, R.Messages.rating_song_superStar);
    public static readonly SongRating Ultrastar = new(ESongRating.Ultrastar, 9000, R.Messages.rating_song_ultraStar);

    public enum ESongRating
    {
        ToneDeaf = 0, Amateur = 1, Wannabe = 2, Hopeful = 3, RisingStar = 4, LeadSinger = 5, Superstar = 6, Ultrastar = 7
    }

    private readonly string i18nCode;
    public ESongRating EnumValue { get; private set; }
    public float ScoreThreshold { get; private set; }

    public string Text
    {
        get
        {
            return TranslationManager.GetTranslation(i18nCode);
        }
    }

    public Color BackgroundColor { get; private set; }

    private static List<SongRating> values;
    public static List<SongRating> Values
    {
        get
        {
            if (values == null)
            {
                values = new List<SongRating>();
                values.Add(Ultrastar);
                values.Add(Superstar);
                values.Add(RisingStar);
                values.Add(Hopeful);
                values.Add(Wannabe);
                values.Add(Amateur);
                values.Add(ToneDeaf);
            }
            return values;
        }
    }

    private SongRating(ESongRating eSongRating, float scoreThreshold, string i18nCode)
    {
        this.EnumValue = eSongRating;
        this.ScoreThreshold = scoreThreshold;
        this.i18nCode = i18nCode;
    }

}
