using System;
using UniRx;
using UnityEngine.UIElements;

public class ToogleButtonControl
{
    private Button button;
    private VisualElement onIcon;
    private VisualElement offIcon;

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

    private Subject<ValueChangedEvent<bool>> valueChangedEventStream = new();
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

        button.RegisterCallbackButtonTriggered(() =>
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
