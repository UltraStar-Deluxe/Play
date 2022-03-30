using System;
using System.Globalization;
using UniRx;
using UnityEngine.InputSystem;

public class NumberPickerControl : ComputedItemPickerControl<float>
{
    private Func<float, string> getLabelTextFunction = item => item.ToString(CultureInfo.InvariantCulture);
    public Func<float, string> GetLabelTextFunction
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
    public float MinValue => ItemPicker.minValue;
    public float MaxValue => ItemPicker.maxValue;
    public float StepValue => ItemPicker.stepValue;

    public NumberPickerControl(ItemPicker itemPicker, float initialValue=0)
        : base(itemPicker, initialValue)
    {
        Selection.Subscribe(newValue => UpdateLabelText(newValue));
    }

    public override void SelectNextItem()
    {
        float currentValue = SelectedItem;
        if (currentValue >= MaxValue)
        {
            if (WrapAround)
            {
                SelectItem(MinValue);
            }
            return;
        }

        float nextValue = currentValue + GetModifiedStepValue();
        if (nextValue > MaxValue)
        {
            nextValue = MaxValue;
        }

        if (nextValue != currentValue)
        {
            SelectItem(nextValue);
        }
    }

    private float GetModifiedStepValue()
    {
        return Keyboard.current != null
               && Keyboard.current.shiftKey.isPressed
            ? StepValue * 10
            : StepValue;
    }

    public override void SelectPreviousItem()
    {
        float currentValue = SelectedItem;
        if (currentValue <= MinValue)
        {
            if (WrapAround)
            {
                SelectItem(MaxValue);
            }
            return;
        }

        float nextValue = currentValue - GetModifiedStepValue();
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

    private void UpdateLabelText(float newValue)
    {
        ItemPicker.ItemLabel.text = GetLabelTextFunction(newValue);
    }
}
