using System;
using System.Collections.Generic;
using UniRx;

public class LabeledItemPickerControl<T> : ListedItemPickerControl<T>
{
    private readonly string smallFontUssClass = "smallFont";
    private readonly Func<T, Translation> getLabelTextFunction;

    public bool AutoSmallFont { get; set; } = true;

    public LabeledItemPickerControl(ItemPicker itemPicker, List<T> items,
         Func<T, Translation> getLabelTextFunction)
        : base(itemPicker)
    {
        this.getLabelTextFunction = getLabelTextFunction;
        Selection.Subscribe(UpdateLabelText);
        Items = items;
        UpdateLabelText(SelectedItem);
    }

    public void UpdateLabelText()
    {
        UpdateLabelText(SelectedItem);
    }

    private void UpdateLabelText(T item)
    {
        ItemPicker.ItemLabel.SetTranslatedText(getLabelTextFunction(item));

        if (AutoSmallFont)
        {
            if (ItemPicker.ItemLabel.text.Length > 28
                || ItemPicker.ItemLabel.text.Contains("\n"))
            {
                ItemPicker.ItemLabel.AddToClassListIfNew(smallFontUssClass);
            }
            else
            {
                ItemPicker.ItemLabel.RemoveFromClassList(smallFontUssClass);
            }
        }
    }
}
