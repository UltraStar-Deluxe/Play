using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class CountingNumberText : MonoBehaviour
{
    private const string DecimalFormat = "0.0";
    private const string IntegerFormat = "0";

    public string NumberFormat { get; set; } = IntegerFormat;

    public double TargetValue { get; set; }
    private double displayedValue;

    private Text text;

    void OnEnable()
    {
        text = GetComponent<Text>();
    }

    void Update()
    {
        displayedValue = GetNextValue(displayedValue, TargetValue);
        DisplayNumber(displayedValue);
    }

    public static double GetNextValue(double currentValue, double targetValue)
    {
        double roundingDistance = 5;
        double distance = targetValue - currentValue;
        if (Math.Abs(distance) < roundingDistance)
        {
            return targetValue;
        }
        else
        {
            double minStep = 3;
            double step = distance / 10.0f;
            if (targetValue > currentValue)
            {
                if (step < minStep)
                {
                    step = minStep;
                }
            }
            else if (targetValue < currentValue)
            {
                if (step > -minStep)
                {
                    step = -minStep;
                }
            }
            return currentValue + step;
        }
    }

    private void DisplayNumber(double value)
    {
        displayedValue = value;
        text.text = value.ToString(NumberFormat);
    }
}
