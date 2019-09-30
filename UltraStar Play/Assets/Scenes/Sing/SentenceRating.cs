using System.Collections.Generic;

public class SentenceRating
{
    private static readonly SentenceRating Perfect = new SentenceRating(0.95, "rating.sentence.perfect");
    private static readonly SentenceRating Cool = new SentenceRating(0.8, "rating.sentence.cool");
    private static readonly SentenceRating Great = new SentenceRating(0.7, "rating.sentence.great");
    private static readonly SentenceRating Good = new SentenceRating(0.6, "rating.sentence.good");
    private static readonly SentenceRating NotBad = new SentenceRating(0.5, "rating.sentence.notbad");
    private static readonly SentenceRating Bad = new SentenceRating(0.3, "rating.sentence.bad");
    private static readonly SentenceRating Poor = new SentenceRating(0.2, "rating.sentence.poor");
    private static readonly SentenceRating Awful = new SentenceRating(0, "rating.sentence.awful");

    private string i18nCode;
    public double PercentageThreshold { get; private set; }

    public string RatingText
    {
        get
        {
            return I18NManager.Instance.GetTranslation(i18nCode);
        }
    }

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

    private SentenceRating(double percentThreshold, string i18nCode)
    {
        this.PercentageThreshold = percentThreshold;
        this.i18nCode = i18nCode;
    }

}