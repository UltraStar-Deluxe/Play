public class PercentNumberPickerControl : UnitNumberPickerControl
{
    public PercentNumberPickerControl(ItemPicker itemPicker, double initialValue = 0)
        : base(itemPicker, "%", initialValue)
    {
    }
}
