using System;
using System.Collections.Generic;
using ProTrans;

public class TranslatedLabeledItemPickerControl<T> : LabeledItemPickerControl<T>, ITranslator
{
    public TranslatedLabeledItemPickerControl(
        ItemPicker itemPicker,
        List<T> items,
        Func<T, string> getTranslationFunction)
        : base(itemPicker, items)
    {
        SetTranslationFunction(getTranslationFunction);
    }

    public void SetTranslationFunction(Func<T, string> getTranslationFunction)
    {
        GetLabelTextFunction = (value) =>
        {
            string translation = getTranslationFunction(value);
            return !translation.IsNullOrEmpty() ? translation : value.ToString();
        };
    }

    public void UpdateTranslation()
    {
        // Update label text. Thereby the translation will be fetched anew.
        UpdateLabelText();
    }
}
