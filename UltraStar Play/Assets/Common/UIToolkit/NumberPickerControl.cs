using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;
using UniRx;
using UnityEngine.InputSystem;

public class NumberPickerControl : ComputedItemPickerControl<double>
{
    private Func<double, string> getLabelTextFunction = item => item.ToString(CultureInfo.InvariantCulture);
    public Func<double, string> GetLabelTextFunction
    {
        get
        {
            return getLabelTextFunction;
        }
        set
        {
            getLabelTextFunction = value;
            UpdateLabelText(SelectedItem);
        }
    }

    public bool WrapAround => ItemPicker.wrapAround;
    public double MinValue => ItemPicker.minValue;
    public double MaxValue => ItemPicker.maxValue;
    public double StepValue => ItemPicker.stepValue;

    public NumberPickerControl(ItemPicker itemPicker, double initialValue=0)
        : base(itemPicker, initialValue)
    {
        Selection.Subscribe(newValue => UpdateLabelText(newValue));
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

    public void UpdateLabelText()
    {
        UpdateLabelText(SelectedItem);
    }

    private void UpdateLabelText(double newValue)
    {
        ItemPicker.ItemLabel.text = GetLabelTextFunction(newValue);
    }
}
