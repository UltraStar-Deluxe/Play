using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class EnumModSettingControl<T> : AbstractModSettingControl<T> where T : struct, Enum
{
    private readonly List<T> availableValues;
    private List<T> AvailableValues
    {
        get
        {
            if (availableValues.IsNullOrEmpty())
            {
                return EnumUtils.GetValuesAsList<T>();
            }

            return availableValues;
        }
    }

    public EnumModSettingControl(Func<T> valueGetter, Action<T> valueSetter, List<T> availableValues = null)
        : base(valueGetter, valueSetter)
    {
        this.availableValues = availableValues;
    }

    public override VisualElement CreateVisualElement()
    {
        DropdownField dropdownField = new();
        dropdownField.label = Label;
        dropdownField.choices = AvailableValues.Select(it => it.ToString()).ToList();

        FieldBindingUtils.Bind<string>(dropdownField, StringValueGetter, StringValueSetter);
        return dropdownField;
    }

    private void StringValueSetter(string newValueAsString)
    {
        if (Enum.TryParse(newValueAsString, out T newValue))
        {
            ValueSetter(newValue);
        }
        else
        {
            Debug.LogWarning($"Failed to parse string '{newValueAsString}' as enum {typeof(T)}");
        }
    }

    private string StringValueGetter()
    {
        T currentValue = ValueGetter();
        string currentValueAsString = currentValue.ToString();
        return currentValueAsString;
    }
}
