using System;
using UnityEngine.UIElements;

public class StringModSettingControl : AbstractModSettingControl<string>
{
    public bool IsMultiline  { get; set; }
    public bool IsPassword  { get; set; }

    public StringModSettingControl(
        Func<string> valueGetter,
        Action<string> valueSetter)
        : base(valueGetter, valueSetter)
    {
    }

    public override VisualElement CreateVisualElement()
    {
        TextField textField = new();
        textField.label = Label ?? "";
        if (IsMultiline)
        {
            textField.multiline = true;
            textField.AddToClassList("multiline");
        }
        textField.isPasswordField = IsPassword;

        FieldBindingUtils.Bind(textField, ValueGetter, ValueSetter);

        return textField;
    }
}
