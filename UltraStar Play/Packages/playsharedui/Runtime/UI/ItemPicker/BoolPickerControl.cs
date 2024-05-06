using System.Collections.Generic;

public class BoolPickerControl : LabeledItemPickerControl<bool>
{
    public BoolPickerControl(ItemPicker itemPicker)
        : base(itemPicker,
            new List<bool> { false, true },
            item => item
                ? Translation.Get("yes")
                : Translation.Get("no"))
    {

    }
}
