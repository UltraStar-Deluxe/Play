using System;
using System.Globalization;
using UniRx;
using UnityEngine.InputSystem;

public class NumberChooserControl : ComputedChooserControl<double>
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

    public bool WrapAround => Chooser.WrapAround
                              || Chooser.NoPreviousButton
                              || Chooser.NoNextButton;
    public double MinValue => Chooser.MinValue;
    public double MaxValue => Chooser.MaxValue;
    public double StepValue => Chooser.StepValue;

    public NumberChooserControl(Chooser chooser, double initialValue=0)
        : base(chooser, initialValue)
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
        Chooser.ItemLabel.text = GetLabelTextFunction(newValue);
    }
}
