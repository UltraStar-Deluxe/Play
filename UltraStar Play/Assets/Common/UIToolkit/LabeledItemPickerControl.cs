using System;
using System.Collections.Generic;
using UniRx;

public class LabeledItemPickerControl<T> : ListedItemPickerControl<T>
{
    public LabeledItemPickerControl(ItemPicker itemPicker, List<T> items)
        : base(itemPicker)
    {
        Selection.Subscribe(UpdateLabelText);
        Items = items;
    }

    public virtual void UpdateLabelText(T item)
    {
        ItemPicker.ItemLabel.text = GetLabelText(item);
    }

    protected virtual string GetLabelText(T item)
    {
        return item != null
            ? item.ToString()
            : "";
    }
}
