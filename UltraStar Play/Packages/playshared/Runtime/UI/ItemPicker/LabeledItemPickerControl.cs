using System;
using System.Collections.Generic;
using UniRx;

public class LabeledItemPickerControl<T> : ListedItemPickerControl<T>
{
    private readonly string smallFontUssClass = "smallFont";

    private Func<T, string> getLabelTextFunction = item => item != null ? item.ToString() : "";

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
        if (ItemPicker.ItemLabel.text.Length > 28
            || ItemPicker.ItemLabel.text.Contains("\n"))
        {
            ItemPicker.AddToClassListIfNew(smallFontUssClass);
        }
        else
        {
            ItemPicker.RemoveFromClassList(smallFontUssClass);
        }
    }
}
