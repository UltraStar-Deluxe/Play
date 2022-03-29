using System;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NumberSpinner : MonoBehaviour, INeedInjection
{
    public bool hasMin;
    public double minValue;

    public bool hasMax;
    public double maxValue = 1;

    public double step = 1;

    public bool wrapAround;

    public string formatString = "F2";

    [InjectedInInspector]
    public Button previousItemButton;

    [InjectedInInspector]
    public Button nextItemButton;

    [InjectedInInspector]
    public Text uiText;

    private readonly Subject<double> selectedValueStream = new Subject<double>();
    public IObservable<double> SelectedValueStream
    {
        get
        {
            return selectedValueStream;
        }
    }

    private double selectedValue;
    public double SelectedValue
    {
        get
        {
            return selectedValue;
        }
        set
        {
            SetNewValueWithinLimits(value);
        }
    }

    protected virtual void Awake()
    {
        SelectedValueStream.Subscribe(UpdateUiText);
    }

    protected virtual void Start()
    {
        nextItemButton.OnClickAsObservable().Subscribe(_ => SelectNextValue());
        previousItemButton.OnClickAsObservable().Subscribe(_ => SelectPreviousValue());
    }

    private void SelectPreviousValue()
    {
        double stepValue = InputUtils.IsKeyboardShiftPressed() ? step * 10 : step;
        SetNewValueWithinLimits(selectedValue - stepValue);
    }

    private void SelectNextValue()
    {
        double stepValue = InputUtils.IsKeyboardShiftPressed() ? step * 10 : step;
        SetNewValueWithinLimits(selectedValue + stepValue);
    }

    private void SetNewValueWithinLimits(double newValue)
    {
        // Wrap around
        if (wrapAround && hasMin && hasMax)
        {
            if (selectedValue == minValue && newValue < minValue)
            {
                newValue = maxValue;
            }
            if (selectedValue == maxValue && newValue > maxValue)
            {
                newValue = minValue;
            }
        }

        // Limit new value to min and max
        if (hasMin && newValue < minValue)
        {
            newValue = minValue;
        }
        if (hasMax && newValue > maxValue)
        {
            newValue = maxValue;
        }

        // Set new value and notify listeners
        selectedValue = newValue;
        selectedValueStream.OnNext(selectedValue);
    }

    protected virtual void UpdateUiText(double newValue)
    {
        uiText.text = newValue.ToString(formatString);
    }
}
