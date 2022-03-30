using System.Collections.Generic;
using ProTrans;
using UnityEngine;

public class SentenceRating
{
    // TODO: Add style sheet classes and define in style sheet
    private const string SentenceRatingColorPerfect = "#3AFF4E7F";
    private const string SentenceRatingColorCool = "#7FE82A7F";
    private const string SentenceRatingColorGreat = "#7FE82A7F";
    private const string SentenceRatingColorGood = "#E4FF1F7F";
    private const string SentenceRatingColorNotBad = "#E4FF1F7F";
    private const string SentenceRatingColorBad = "#FF9C4F7F";
    private const string SentenceRatingColorPoor = "#E848E67F";
    private const string SentenceRatingColorAwful = "#764FFF7F";

    public static readonly SentenceRating perfect = new(0.95f, R.Messages.rating_sentence_perfect, SentenceRatingColorPerfect);
    public static readonly SentenceRating cool = new(0.8f, R.Messages.rating_sentence_cool, SentenceRatingColorCool);
    public static readonly SentenceRating great = new(0.7f, R.Messages.rating_sentence_great, SentenceRatingColorGreat);
    public static readonly SentenceRating good = new(0.6f, R.Messages.rating_sentence_good, SentenceRatingColorGood);
    public static readonly SentenceRating notBad = new(0.5f, R.Messages.rating_sentence_notBad, SentenceRatingColorNotBad);
    public static readonly SentenceRating bad = new(0.3f, R.Messages.rating_sentence_bad, SentenceRatingColorBad);
    public static readonly SentenceRating poor = new(0.2f, R.Messages.rating_sentence_poor, SentenceRatingColorPoor);
    public static readonly SentenceRating awful = new(0f, R.Messages.rating_sentence_awful, SentenceRatingColorAwful);

    private readonly string i18nCode;
    public float PercentageThreshold { get; private set; }

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
                values.Add(cool);
                values.Add(great);
                values.Add(good);
                values.Add(notBad);
                values.Add(bad);
                values.Add(poor);
                values.Add(awful);
            }
            return values;
        }
    }

    private SentenceRating(float percentThreshold, string i18nCode, string hexBackgroundColor)
    {
        this.PercentageThreshold = percentThreshold;
        this.i18nCode = i18nCode;
        BackgroundColor = Colors.CreateColor(hexBackgroundColor);
    }
}
