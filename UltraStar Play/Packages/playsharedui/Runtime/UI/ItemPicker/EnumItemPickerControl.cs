using System.Collections.Generic;

public class EnumItemPickerControl<T> : LabeledItemPickerControl<T>
{
    public EnumItemPickerControl(ItemPicker itemPicker)
        : this(itemPicker, EnumUtils.GetValuesAsList<T>())
    {
    }

    public EnumItemPickerControl(ItemPicker itemPicker, List<T> items)
        : base(itemPicker, items)
    {
        GetLabelTextFunction = item => StringUtils.ToTitleCase(item.ToString());
    }
}
