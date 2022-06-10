using System.Collections.Generic;
using ProTrans;
using UnityEngine;

public class SentenceRating
{
    // TODO: Add style sheet classes and define in style sheet
    private const string SentenceRatingColorPerfect = "#3AFF4EAF";
    private const string SentenceRatingColorGreat = "#20CF327F";
    private const string SentenceRatingColorGood = "#E7B41C7F";
    private const string SentenceRatingColorNotBad = "#44ABDC7F";
    private const string SentenceRatingColorBad = "#961CE77F";

    public static readonly SentenceRating perfect = new(0.95, R.Messages.rating_sentence_perfect, SentenceRatingColorPerfect);
    public static readonly SentenceRating great = new(0.75, R.Messages.rating_sentence_great, SentenceRatingColorGreat);
    public static readonly SentenceRating good = new(0.50, R.Messages.rating_sentence_good, SentenceRatingColorGood);
    public static readonly SentenceRating notBad = new(0.25, R.Messages.rating_sentence_notBad, SentenceRatingColorNotBad);
    public static readonly SentenceRating bad = new(0, R.Messages.rating_sentence_bad, SentenceRatingColorBad);

    private readonly string i18nCode;
    public double PercentageThreshold { get; private set; }

    public string Text
    {
        get
        {
            return TranslationManager.GetTranslation(i18nCode);
        }
    }

    public Color BackgroundColor { get; private set; }

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

    private SentenceRating(double percentThreshold, string i18nCode, string hexBackgroundColor)
    {
        this.PercentageThreshold = percentThreshold;
        this.i18nCode = i18nCode;
        BackgroundColor = Colors.CreateColor(hexBackgroundColor);
    }
}
