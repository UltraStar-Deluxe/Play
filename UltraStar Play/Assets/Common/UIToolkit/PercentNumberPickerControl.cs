using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using UniRx;

public class PercentNumberPickerControl : NumberPickerControl
{
    public PercentNumberPickerControl(ItemPicker itemPicker, double initialValue = 0)
        : base(itemPicker, initialValue)
    {
    }

    protected override string GetLabelText(double newValue)
    {
        return base.GetLabelText(newValue) + " %";
    }
}
