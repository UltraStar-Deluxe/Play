public class PercentNumberPickerControl : NumberPickerControl
{
    public PercentNumberPickerControl(ItemPicker itemPicker, double initialValue = 0)
        : base(itemPicker, initialValue)
    {
        GetLabelTextFunction = item => item + " %";
    }
}
