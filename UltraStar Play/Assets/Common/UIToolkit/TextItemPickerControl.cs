using System;
using System.Collections.Generic;
using UniRx;

public class TextItemPickerControl<T> : ItemPickerControl<T>
{
    public TextItemPickerControl(ItemPicker itemPicker, List<T> items)
        : base(itemPicker)
    {
        Selection.Subscribe(UpdateLabelText);
        Items = items;
    }

    public void UpdateLabelText(T item)
    {
        ItemPicker.ItemLabel.text = GetDisplayText(item);
    }

    protected virtual string GetDisplayText(T item)
    {
        return item != null
            ? item.ToString()
            : "";
    }
}
