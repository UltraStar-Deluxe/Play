using System;
using UnityEngine.UIElements;

public abstract class AbstractModSettingControl<T> : IModSettingControl
{
    public string Label { get; set; }
    public bool IsRequired { get; set; }

    public Func<T> ValueGetter  { get; private set; }
    public Action<T> ValueSetter { get; private set; }

    protected AbstractModSettingControl(Func<T> valueGetter, Action<T> valueSetter)
    {
        ValueGetter = valueGetter;
        ValueSetter = valueSetter;
    }

    public abstract VisualElement CreateVisualElement();
}
