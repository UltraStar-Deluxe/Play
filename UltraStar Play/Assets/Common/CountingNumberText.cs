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
        double roundingDistance = 5;
        double distance = TargetValue - displayedValue;
        if (Math.Abs(distance) < roundingDistance)
        {
            DisplayNumber(TargetValue);
        }
        else
        {
            double minStep = 3;
            double step = distance / 10.0f;
            if (TargetValue > displayedValue)
            {
                if (step < minStep)
                {
                    step = minStep;
                }
            }
            else if (TargetValue < displayedValue)
            {
                if (step > -minStep)
                {
                    step = -minStep;
                }
            }
            DisplayNumber(displayedValue + step);
        }
    }

    private void DisplayNumber(double value)
    {
        displayedValue = value;
        text.text = value.ToString(NumberFormat);
    }
}
