using System;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class FieldBindingUtils
{
    public static void Bind<T>(GameObject gameObject, BaseField<T> baseField,Func<T> valueGetter, Action<T> valueSetter, bool observeValueGetter = true)
    {
        baseField.value = valueGetter();
        baseField.RegisterValueChangedCallback(evt => valueSetter(evt.newValue));

        // Update field when settings change.
        if (observeValueGetter)
        {
            gameObject.ObserveEveryValueChanged(_ => valueGetter())
                .Where(newValue => !object.Equals(baseField.value, newValue))
                .Subscribe(newValue => baseField.value = newValue)
                .AddTo(gameObject);
        }
    }
}
