using System.Collections.Generic;

public class TargetFpsItemPickerControl : LabeledItemPickerControl<int>
{
    public TargetFpsItemPickerControl(ItemPicker itemPicker)
        : base(itemPicker,
            new List<int>(){ -1, 30, 60 },
            newValue => newValue <= 0
                ? Translation.Get(R.Messages.options_sampleRate_auto)
                : Translation.Of(newValue.ToString()))
    {
    }
}
