using System.Collections.Generic;
using UnityEngine;

public class SentenceRating
{
    public static readonly SentenceRating Perfect = new SentenceRating(0.95, "rating.sentence.perfect", "3AFF4E");
    public static readonly SentenceRating Cool = new SentenceRating(0.8, "rating.sentence.cool", "7FE82A");
    public static readonly SentenceRating Great = new SentenceRating(0.7, "rating.sentence.great", "7FE82A");
    public static readonly SentenceRating Good = new SentenceRating(0.6, "rating.sentence.good", "E4FF1F");
    public static readonly SentenceRating NotBad = new SentenceRating(0.5, "rating.sentence.notbad", "E4FF1F");
    public static readonly SentenceRating Bad = new SentenceRating(0.3, "rating.sentence.bad", "FF9C4F");
    public static readonly SentenceRating Poor = new SentenceRating(0.2, "rating.sentence.poor", "E848E6");
    public static readonly SentenceRating Awful = new SentenceRating(0, "rating.sentence.awful", "764FFF");

    private string i18nCode;
    public double PercentageThreshold { get; private set; }

    public string Text
    {
        get
        {
            return I18NManager.Instance.GetTranslation(i18nCode);
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
        if (ColorUtility.TryParseHtmlString("#" + hexBackgroundColor, out Color backgroundColor))
        {
            backgroundColor.a = 0.5f;
            BackgroundColor = backgroundColor;
        }
        else
        {
            Debug.LogError("Parsing hex color failed: " + hexBackgroundColor);
        }
    }

}