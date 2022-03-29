using ProTrans;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongOrderPickerControl : LabeledItemPickerControl<ESongOrder>
{
    public SongOrderPickerControl(ItemPicker itemPicker)
        : base(itemPicker, EnumUtils.GetValuesAsList<ESongOrder>())
    {
        GetLabelTextFunction = (songOrder) =>
        {
            string prefixTranslation = TranslationManager.GetTranslation(R.Messages.order);
            string valueTranslation = songOrder.GetTranslatedName();
            return $"{prefixTranslation}:\n{valueTranslation}";
        };
    }
}
