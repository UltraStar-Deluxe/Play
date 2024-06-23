using System.Collections.Generic;

public class SentenceRating
{
    public static readonly SentenceRating perfect = new(ESentenceRating.Perfect, 0.95, R.Messages.rating_sentence_perfect);
    public static readonly SentenceRating great = new(ESentenceRating.Great, 0.75, R.Messages.rating_sentence_great);
    public static readonly SentenceRating good = new(ESentenceRating.Good, 0.50, R.Messages.rating_sentence_good);
    public static readonly SentenceRating notBad = new(ESentenceRating.NotBad, 0.25, R.Messages.rating_sentence_notBad);
    public static readonly SentenceRating bad = new(ESentenceRating.Bad, 0, R.Messages.rating_sentence_bad);

    private readonly string translationKey;
    public ESentenceRating EnumValue { get; private set; }
    public double PercentageThreshold { get; private set; }

    public Translation Translation => Translation.Get(translationKey);

    private static List<SentenceRating> values;
    public static List<SentenceRating> Values
    {
        get
        {
            if (values == null)
            {
                values = new List<SentenceRating>();
                values.Add(perfect);
                values.Add(great);
                values.Add(good);
                values.Add(notBad);
                values.Add(bad);
            }
            return values;
        }
    }

    private SentenceRating(ESentenceRating enumValue, double percentThreshold, string translationKey)
    {
        this.EnumValue = enumValue;
        this.PercentageThreshold = percentThreshold;
        this.translationKey = translationKey;
    }

    public static SentenceRating GetSentenceRating(double correctNotesPercentage)
    {
        if (correctNotesPercentage < 0)
        {
            return null;
        }

        foreach (SentenceRating sentenceRating in Values)
        {
            if (correctNotesPercentage >= sentenceRating.PercentageThreshold)
            {
                return sentenceRating;
            }
        }
        return null;
    }
}
