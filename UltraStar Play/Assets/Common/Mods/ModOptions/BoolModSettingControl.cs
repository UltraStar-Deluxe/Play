using System;
using UnityEngine.UIElements;

public class BoolModSettingControl : AbstractModSettingControl<bool>
{
    public BoolModSettingControl(Func<bool> valueGetter, Action<bool> valueSetter)
        : base(valueGetter, valueSetter)
    {
    }

    public override VisualElement CreateVisualElement()
    {
        Toggle toggle = new();
        toggle.label = Label ?? "";

        FieldBindingUtils.Bind(toggle, ValueGetter, ValueSetter);
        return toggle;
    }
}
