public class TranslationResult
{
    private readonly string value;

    private TranslationResult(string value)
    {
        this.value = value ?? "";
    }

    public static implicit operator string(TranslationResult it) => it?.value ?? "";

    public static TranslationResult Of(string value)
    {
        return new TranslationResult(value);
    }
}
