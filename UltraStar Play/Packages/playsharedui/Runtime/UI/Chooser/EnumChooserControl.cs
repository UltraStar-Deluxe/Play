using System;
using System.Collections.Generic;

public class EnumChooserControl<T> : LabeledChooserControl<T> where T : Enum
{
    public EnumChooserControl(Chooser chooser)
        : this(chooser, EnumUtils.GetValuesAsList<T>())
    {
    }

    public EnumChooserControl(Chooser chooser, List<T> items)
        : base(chooser, items, item => Translation.Get(item))
    {
    }
}
