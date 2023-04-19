using System;
using UnityEngine.UIElements;

public class BaseFieldWithTextValueFieldControl<T>
{
    public BaseFieldWithTextValueFieldControl(BaseField<T> baseField, TextValueField<T> textField)
    {
        // Set TextField when BaseField changes.
        baseField.RegisterValueChangedCallback(evt =>
        {
            if (!Equals(textField.value, evt.newValue))
            {
                textField.value = evt.newValue;
            }
        });
        
        // Set BaseField when TextField changes.
        textField.RegisterValueChangedCallback(evt =>
        {
            if (!Equals(baseField.value, evt.newValue))
            {
                baseField.value = evt.newValue;
            }
        });
    }
}
