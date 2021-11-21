using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using UniRx;
using UnityEngine.InputSystem;

public class NumberPickerControl : ComputedItemPickerControl<double>
{
    public bool WrapAround => ItemPicker.wrapAround;
    public double MinValue => ItemPicker.minValue;
    public double MaxValue => ItemPicker.maxValue;
    public double StepValue => ItemPicker.stepValue;

    public NumberPickerControl(ItemPicker itemPicker, double initialValue=0)
        : base(itemPicker, initialValue)
    {
        Selection.Subscribe(newValue => ItemPicker.ItemLabel.text = GetLabelText(newValue));
    }

    protected virtual string GetLabelText(double newValue)
    {
        return newValue.ToString(CultureInfo.InvariantCulture);
    }

    public override void SelectNextItem()
    {
        double currentValue = SelectedItem;
        if (currentValue >= MaxValue)
        {
            if (WrapAround)
            {
                SelectItem(MinValue);
            }
            return;
        }

        double nextValue = currentValue + GetModifiedStepValue();
        if (nextValue > MaxValue)
        {
            nextValue = MaxValue;
        }

        if (nextValue != currentValue)
        {
            SelectItem(nextValue);
        }
    }

    private double GetModifiedStepValue()
    {
        return Keyboard.current != null
               && Keyboard.current.shiftKey.isPressed
            ? StepValue * 10
            : StepValue;
    }

    public override void SelectPreviousItem()
    {
        double currentValue = SelectedItem;
        if (currentValue <= MinValue)
        {
            if (WrapAround)
            {
                SelectItem(MaxValue);
            }
            return;
        }

        double nextValue = currentValue - GetModifiedStepValue();
        if (nextValue < MinValue)
        {
            nextValue = MinValue;
        }

        if (nextValue != currentValue)
        {
            SelectItem(nextValue);
        }
    }
}
