using System.Collections.Generic;
using UnityEngine;

public class SentenceRating
{
    public static readonly SentenceRating Perfect = new SentenceRating(0.95, R.String.rating_sentence_perfect, R.Color.rating_sentence_perfect);
    public static readonly SentenceRating Cool = new SentenceRating(0.8, R.String.rating_sentence_cool, R.Color.rating_sentence_cool);
    public static readonly SentenceRating Great = new SentenceRating(0.7, R.String.rating_sentence_great, R.Color.rating_sentence_great);
    public static readonly SentenceRating Good = new SentenceRating(0.6, R.String.rating_sentence_good, R.Color.rating_sentence_good);
    public static readonly SentenceRating NotBad = new SentenceRating(0.5, R.String.rating_sentence_notBad, R.Color.rating_sentence_notBad);
    public static readonly SentenceRating Bad = new SentenceRating(0.3, R.String.rating_sentence_bad, R.Color.rating_sentence_bad);
    public static readonly SentenceRating Poor = new SentenceRating(0.2, R.String.rating_sentence_poor, R.Color.rating_sentence_poor);
    public static readonly SentenceRating Awful = new SentenceRating(0, R.String.rating_sentence_awful, R.Color.rating_sentence_awful);

    private readonly string i18nCode;
    public double PercentageThreshold { get; private set; }

    public string Text
    {
        get
        {
            return I18NManager.GetTranslation(i18nCode);
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
                values.Add(Perfect);
                values.Add(Cool);
                values.Add(Great);
                values.Add(Good);
                values.Add(NotBad);
                values.Add(Bad);
                values.Add(Poor);
                values.Add(Awful);
            }
            return values;
        }
    }

    private SentenceRating(double percentThreshold, string i18nCode, string hexBackgroundColor)
    {
        this.PercentageThreshold = percentThreshold;
        this.i18nCode = i18nCode;
        BackgroundColor = ThemeManager.GetColor(hexBackgroundColor);
    }
}
