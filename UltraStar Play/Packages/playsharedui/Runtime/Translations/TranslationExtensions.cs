using UnityEngine.UIElements;

public static class TranslationExtensions
{
    public static void SetTranslatedLabel<T>(this BaseField<T> baseField, Translation text)
    {
        baseField.label = text;
    }

    public static void SetTranslatedLabel(this ItemPicker itemPicker, Translation text)
    {
        itemPicker.Label = text;
    }

    public static void SetTranslatedTitle(this AccordionItem accordionItem, Translation text)
    {
        accordionItem.Title = text;
    }

    public static void SetTranslatedText(this TextElement textElement, Translation text)
    {
        textElement.text = text;
    }
}
