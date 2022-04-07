using System.Collections.Generic;
using ProTrans;

public class BoolPickerControl : LabeledItemPickerControl<bool>
{
    public BoolPickerControl(ItemPicker itemPicker)
        : base(itemPicker, new List<bool> { false, true })
    {
        GetLabelTextFunction = item =>
        {
            if (item)
            {
                return TranslationManager.GetTranslation(R.Messages.yes);
            }
            else
            {
                return TranslationManager.GetTranslation(R.Messages.no);
            }
        };
    }
}
