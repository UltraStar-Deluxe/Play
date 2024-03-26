public class TranslatedText
{
    public string Value { get; set; }

    public TranslatedText(string value)
    {
        Value = value;
    }

    public static implicit operator string(TranslatedText it) => it.Value;
}
