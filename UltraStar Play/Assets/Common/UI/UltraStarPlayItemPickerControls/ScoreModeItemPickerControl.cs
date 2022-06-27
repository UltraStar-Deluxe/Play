public class ScoreModeItemPickerControl : TranslatedLabeledItemPickerControl<EScoreMode>
{
    public ScoreModeItemPickerControl(ItemPicker itemPicker)
        : base(itemPicker,
            EnumUtils.GetValuesAsList<EScoreMode>(),
            scoreMode => scoreMode.GetTranslation())
    {
    }
}
