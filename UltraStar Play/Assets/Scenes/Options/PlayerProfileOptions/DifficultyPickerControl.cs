public class DifficultyPicker : LabeledItemPickerControl<EDifficulty>
{
    public DifficultyPicker(ItemPicker itemPicker)
        : base(itemPicker, EnumUtils.GetValuesAsList<EDifficulty>())
    {
        GetLabelTextFunction = item => item.GetTranslatedName();
    }
}
