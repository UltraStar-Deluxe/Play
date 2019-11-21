using System.Collections.Generic;
using UnityEngine;

public class SongRating
{
    public static readonly SongRating ToneDeaf = new SongRating(ESongRating.ToneDeaf, 0, I18NKeys.rating_song_tone_deaf);
    public static readonly SongRating Amateur = new SongRating(ESongRating.Amateur, 2000, I18NKeys.rating_song_amateur);
    public static readonly SongRating Wannabe = new SongRating(ESongRating.Wannabe, 4000, I18NKeys.rating_song_wannabe);
    public static readonly SongRating Hopeful = new SongRating(ESongRating.Hopeful, 5000, I18NKeys.rating_song_hopeful);
    public static readonly SongRating RisingStar = new SongRating(ESongRating.RisingStar, 6000, I18NKeys.rating_song_rising_star);
    public static readonly SongRating LeadSinger = new SongRating(ESongRating.LeadSinger, 7500, I18NKeys.rating_song_lead_singer);
    public static readonly SongRating Superstar = new SongRating(ESongRating.Superstar, 8500, I18NKeys.rating_song_superstar);
    public static readonly SongRating Ultrastar = new SongRating(ESongRating.Ultrastar, 9000, I18NKeys.rating_song_ultrastar);

    public enum ESongRating
    {
        ToneDeaf = 0, Amateur = 1, Wannabe = 2, Hopeful = 3, RisingStar = 4, LeadSinger = 5, Superstar = 6, Ultrastar = 7
    }

    private string i18nCode;
    public ESongRating EnumValue { get; private set; }
    public double ScoreThreshold { get; private set; }

    public string Text
    {
        get
        {
            return I18NManager.Instance.GetTranslation(i18nCode);
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

    private SongRating(ESongRating eSongRating, double scoreThreshold, string i18nCode)
    {
        this.EnumValue = eSongRating;
        this.ScoreThreshold = scoreThreshold;
        this.i18nCode = i18nCode;
    }

}