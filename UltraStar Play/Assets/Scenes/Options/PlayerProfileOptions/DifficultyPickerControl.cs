using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DifficultyPicker : LabeledItemPickerControl<EDifficulty>
{
    public DifficultyPicker(ItemPicker itemPicker)
        : base(itemPicker, EnumUtils.GetValuesAsList<EDifficulty>())
    {
    }

    protected override string GetLabelText(EDifficulty item)
    {
        return item.GetTranslatedName();
    }
}
