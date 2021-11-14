using System.Collections.Generic;
using ProTrans;

public class BoolItemPickerControl : TextItemPickerControl<bool>
{
    public BoolItemPickerControl(ItemPicker itemPicker)
        : base(itemPicker, new List<bool> { false, true })
    {
    }

    protected override string GetDisplayText(bool item)
    {
        if (item)
        {
            return TranslationManager.GetTranslation(R.Messages.yes);
        }
        else
        {
            return TranslationManager.GetTranslation(R.Messages.no);
        }
    }
}
