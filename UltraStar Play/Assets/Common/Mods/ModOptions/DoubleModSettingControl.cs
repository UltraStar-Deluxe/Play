using System;
using UnityEngine.UIElements;

public class DoubleModSettingControl : AbstractModSettingControl<double>
{
    public DoubleModSettingControl(Func<double> valueGetter, Action<double> valueSetter)
        : base(valueGetter, valueSetter)
    {
    }

    public override VisualElement CreateVisualElement()
    {
        DoubleField doubleField = new();
        doubleField.label = Label ?? "";

        FieldBindingUtils.Bind(doubleField, ValueGetter, ValueSetter);

        return doubleField;
    }
}
