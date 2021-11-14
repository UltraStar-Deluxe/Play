using System;
using System.Collections.Generic;
using UniRx;

abstract public class ComputedItemPickerControl<T> : AbstractItemPickerControl<T>
{
    public ComputedItemPickerControl(ItemPicker itemPicker, T initialValue)
        : base(itemPicker)
    {
        SelectItem(initialValue);
    }
}
