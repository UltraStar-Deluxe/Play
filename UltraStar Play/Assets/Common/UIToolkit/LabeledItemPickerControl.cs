using System;
using System.Collections.Generic;
using UniRx;

public class LabeledItemPickerControl<T> : ListedItemPickerControl<T>
{
    private Func<T, string> getLabelTextFunction = (T item) => item != null ? item.ToString() : "";
    public Func<T, string> GetLabelTextFunction
    {
        get
        {
            return getLabelTextFunction;
        }
        set
        {
            getLabelTextFunction = value;
            UpdateLabelText(SelectedItem);
        }
    }

    public LabeledItemPickerControl(ItemPicker itemPicker, List<T> items)
        : base(itemPicker)
    {
        Selection.Subscribe(UpdateLabelText);
        Items = items;
    }

    public void UpdateLabelText()
    {
        UpdateLabelText(SelectedItem);
    }

    private void UpdateLabelText(T item)
    {
        ItemPicker.ItemLabel.text = GetLabelTextFunction(item);
    }
}
