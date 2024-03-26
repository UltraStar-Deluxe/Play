using UnityEngine.UIElements;

public static class TranslationExtensions
{
    public static void SetLabel<T>(this BaseField<T> baseField, TranslatedText text)
    {
        baseField.label = text;
    }
}
