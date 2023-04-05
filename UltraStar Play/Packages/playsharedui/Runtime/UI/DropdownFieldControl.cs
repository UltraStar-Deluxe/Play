using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine.UIElements;

public class DropdownFieldControl<T>
{
    private readonly DropdownField dropdownField;
    private readonly Func<T, string> itemToString;

    private List<T> items;

    public List<T> Items
    {
        get => items;
        set
        {
            items = value;
            dropdownField.choices = items
                .Select(item => itemToString(item))
                .ToList();
            if (items.IsNullOrEmpty()
                || !items.Contains(SelectedItem))
            {
                dropdownField.value = dropdownField.choices.FirstOrDefault();
            }
        }
    }

    public ReactiveProperty<T> Selection { get; private set; }
    public T SelectedItem => Selection.Value;

    public DropdownFieldControl(DropdownField dropdownField, List<T> items, T initialSelection,
        Func<T, string> itemToString)
    {
        this.dropdownField = dropdownField;
        this.Items = items;
        this.itemToString = itemToString;
        Selection = new ReactiveProperty<T>(initialSelection);

        this.dropdownField.choices = items
            .Select(item => this.itemToString(item))
            .ToList();

        if (initialSelection != null)
        {
            this.dropdownField.value = this.dropdownField.choices
                .FirstOrDefault(itemAsString => itemToString(initialSelection) == itemAsString);
        }

        this.dropdownField.RegisterValueChangedCallback(evt =>
        {
            T newValue = Items.FirstOrDefault(item => itemToString(item) == dropdownField.value);
            if (!Equals(Selection.Value, newValue))
            {
                SetSelection(newValue);
            }
        });

        Selection.Subscribe(newValue =>
        {
            if (newValue == null)
            {
                if (this.dropdownField.value != null)
                {
                    this.dropdownField.value = null;
                }
            }
            else if (newValue.ToString() != dropdownField.value)
            {
                dropdownField.value = newValue.ToString();
            }
        });
    }

    public void SetSelection(T newValue)
    {
        Selection.Value = newValue;
    }
}
