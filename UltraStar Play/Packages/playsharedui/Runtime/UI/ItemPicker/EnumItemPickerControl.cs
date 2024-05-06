using System;
using System.Collections.Generic;

public class EnumItemPickerControl<T> : LabeledItemPickerControl<T> where T : Enum
{
    public EnumItemPickerControl(ItemPicker itemPicker)
        : this(itemPicker, EnumUtils.GetValuesAsList<T>())
    {
    }

    public EnumItemPickerControl(ItemPicker itemPicker, List<T> items)
        : base(itemPicker, items, item => Translation.Get(item))
    {
    }
}
