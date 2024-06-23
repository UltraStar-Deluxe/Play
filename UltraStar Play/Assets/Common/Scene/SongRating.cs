using System.Collections.Generic;

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

    private readonly string translationKey;
    public ESongRating EnumValue { get; private set; }
    public double ScoreThreshold { get; private set; }

    public Translation Translation => Translation.Get(translationKey);

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
                values.Add(LeadSinger);
                values.Add(RisingStar);
                values.Add(Hopeful);
                values.Add(Wannabe);
                values.Add(Amateur);
                values.Add(ToneDeaf);
            }
            return values;
        }
    }

    private SongRating(ESongRating eSongRating, double scoreThreshold, string translationKey)
    {
        this.EnumValue = eSongRating;
        this.ScoreThreshold = scoreThreshold;
        this.translationKey = translationKey;
    }

}
