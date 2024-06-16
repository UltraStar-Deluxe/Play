using System;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class FieldBindingUtils
{
    public static void Bind<T>(BaseField<T> baseField, Func<T> valueGetter, Action<T> valueSetter)
    {
        Bind(null, baseField, valueGetter, valueSetter, false);
    }

    public static void Bind<T>(GameObject gameObject, BaseField<T> baseField, Func<T> valueGetter, Action<T> valueSetter, bool observeValueGetter = true)
    {
        baseField.value = valueGetter();
        baseField.RegisterValueChangedCallback(evt =>
        {
            if (evt.target is Label)
            {
               // Workaround for Unity, which fires a ChangeEvent for the label ( https://forum.unity.com/threads/proper-way-to-get-changed-values-from-a-textfield.1432954/ )
                return;
            }
            valueSetter(evt.newValue);
        });

        // Update field when settings change.
        if (observeValueGetter)
        {
            gameObject.ObserveEveryValueChanged(_ => valueGetter())
                .Where(newValue => !object.Equals(baseField.value, newValue))
                .Subscribe(newValue => baseField.value = newValue)
                .AddTo(gameObject);
        }
    }

    public static void ResetValueOnBlurIfEmpty<T>(
        BaseField<T> baseField)
    {
        T emptyValue = default(T);
        bool IsEmptyValue(T value)
        {
            return object.Equals(value, emptyValue)
                   || value is "";
        }

        T valueBeforeEditing = baseField.value;
        baseField.RegisterCallback<FocusEvent>(evt =>
        {
            if (!IsEmptyValue(baseField.value))
            {
                // Remember this value
                valueBeforeEditing = baseField.value;
            }
        });
        baseField.RegisterCallback<BlurEvent>(evt =>
        {
            if (IsEmptyValue(baseField.value))
            {
                // Reset value
                baseField.value = valueBeforeEditing;
            }
        });
    }
}
