public class UnitNumberPickerControl : NumberPickerControl
{
    public UnitNumberPickerControl(ItemPicker itemPicker, string unit, double initialValue = 0)
        : base(itemPicker, initialValue)
    {
        GetLabelTextFunction = item => item + $" {unit}";
    }
}
