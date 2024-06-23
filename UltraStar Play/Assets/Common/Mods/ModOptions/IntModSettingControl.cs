using System;
using UnityEngine.UIElements;

public class IntModSettingControl : AbstractModSettingControl<int>
{
    public IntModSettingControl(Func<int> valueGetter, Action<int> valueSetter)
        : base(valueGetter, valueSetter)
    {
    }

    public override VisualElement CreateVisualElement()
    {
        IntegerField integerField = new();
        integerField.label = Label ?? "";

        FieldBindingUtils.Bind(integerField, ValueGetter, ValueSetter);

        return integerField;
    }
}
