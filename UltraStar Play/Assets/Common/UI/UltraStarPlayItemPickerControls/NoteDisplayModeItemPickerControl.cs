public class NoteDisplayModeItemPickerControl : TranslatedLabeledItemPickerControl<ENoteDisplayMode>
{
    public NoteDisplayModeItemPickerControl(ItemPicker itemPicker)
        : base(itemPicker,
            EnumUtils.GetValuesAsList<ENoteDisplayMode>(),
            noteDisplayMode => noteDisplayMode.GetTranslation())
    {
    }
}
