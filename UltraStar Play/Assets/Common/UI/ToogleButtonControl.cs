using System;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class ToogleButtonControl
{
    private readonly Button button;
    private readonly VisualElement onIcon;
    private readonly VisualElement offIcon;

    private bool isOn;
    public bool IsOn
    {
        get
        {
            return isOn;
        }
        set
        {
            isOn = value;
            UpdateIcon();
        }
    }

    private readonly Subject<ValueChangedEvent<bool>> valueChangedEventStream = new();
    public IObservable<ValueChangedEvent<bool>> ValueChangedEventStream => valueChangedEventStream;

    public ToogleButtonControl(
        Button button,
        VisualElement onIcon,
        VisualElement offIcon,
        bool initialIsOn)
    {
        this.button = button;
        this.onIcon = onIcon;
        this.offIcon = offIcon;
        IsOn = initialIsOn;

        button.RegisterCallbackButtonTriggered(_ =>
        {
            bool oldValue = IsOn;
            bool newValue = !IsOn;
            IsOn = newValue;
            valueChangedEventStream.OnNext(new ValueChangedEvent<bool>(oldValue, newValue));
        });
    }

    private void UpdateIcon()
    {
        onIcon?.SetVisibleByDisplay(IsOn);
        offIcon?.SetVisibleByDisplay(!IsOn);
    }
}
