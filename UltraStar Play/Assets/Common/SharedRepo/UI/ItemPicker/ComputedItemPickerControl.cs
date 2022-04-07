public abstract class ComputedItemPickerControl<T> : AbstractItemPickerControl<T>
{
    protected ComputedItemPickerControl(ItemPicker itemPicker, T initialValue)
        : base(itemPicker)
    {
        SelectItem(initialValue);
    }
}
