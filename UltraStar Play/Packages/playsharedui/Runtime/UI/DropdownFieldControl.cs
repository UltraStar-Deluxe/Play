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
            if (items.IsNullOrEmpty())
            {
                dropdownField.choices = new();
            }
            else
            {
                dropdownField.choices = items
                    .Select(item => itemToString(item))
                    .ToList();
            }

            if (items.IsNullOrEmpty()
                || !items.Contains(Selection))
            {
                dropdownField.value = dropdownField.choices.FirstOrDefault();
            }
        }
    }

    private readonly ReactiveProperty<T> selectionProperty;
    public IObservable<T> SelectionAsObservable => selectionProperty;

    public T Selection
    {
        get => selectionProperty.Value;
        set
        {
            if (Equals(Selection, value))
            {
                return;
            }
            selectionProperty.Value = value;
        }
    }

    public bool EnableRichText
    {
        get => TextElement.enableRichText;
        set => TextElement.enableRichText = value;
    }

    private TextElement TextElement => dropdownField.Q<TextElement>(); 
    
    public DropdownFieldControl(DropdownField dropdownField, List<T> items, T initialSelection,
        Func<T, string> itemToString)
    {
        this.dropdownField = dropdownField ?? throw new ArgumentNullException(nameof(dropdownField));
        this.itemToString = itemToString ?? throw new ArgumentNullException(nameof(itemToString));
        this.selectionProperty = new ReactiveProperty<T>(initialSelection);
        this.Items = items;

        UpdateChoices();

        if (initialSelection != null)
        {
            this.dropdownField.value = this.dropdownField.choices
                .FirstOrDefault(itemAsString => itemToString(initialSelection) == itemAsString);
        }

        this.dropdownField.RegisterValueChangedCallback(evt =>
        {
            T newValue = Items.FirstOrDefault(item => itemToString(item) == dropdownField.value);
            if (!Equals(Selection, newValue))
            {
                Selection = newValue;
            }
        });

        SelectionAsObservable.Subscribe(newValue =>
        {
            if (newValue == null)
            {
                if (this.dropdownField.value != null)
                {
                    this.dropdownField.value = null;
                }
            }
            else if (this.itemToString(newValue) != dropdownField.value)
            {
                dropdownField.value = this.itemToString(newValue);
            }
        });
        
        this.dropdownField.formatListItemCallback = FormatListItemCallback;
        EnableRichText = false;
    }

    private string FormatListItemCallback(string item)
    {
        // Disable parsing of Rich Text in PopupField labels.
        // This is needed because Rich Text parsing is enabled by default in Unity.
        // See https://discussions.unity.com/t/how-to-disable-rich-text-of-dropdownfield-popup-labels/1690284/3
        return EnableRichText
            ? item
            : $"<noparse>{item}</noparse>";
    }

    public void UpdateLabelText()
    {
        // Dropdown value is determined by the item display text. Thus, the choices must be updated as well as the display text.
        UpdateChoices();
        dropdownField.SetValueWithoutNotify(this.itemToString(Selection));
    }

    private void UpdateChoices()
    {
        this.dropdownField.choices = items
            .Select(item => this.itemToString(item))
            .ToList();
    }
}
