using System.Collections.Generic;

public class BoolPickerControl : LabeledItemPickerControl<bool>
{
    public BoolPickerControl(ItemPicker itemPicker)
        : base(itemPicker, new List<bool> { false, true })
    {
        GetLabelTextFunction = item =>
        {
            if (item)
            {
                return Translation.Get("yes");
            }
            else
            {
                return Translation.Get("no");
            }
        };
    }
}
