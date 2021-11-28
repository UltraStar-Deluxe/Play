using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine.UIElements;

abstract public class PicturedItemPickerControl<T> : ListedItemPickerControl<T>
{
    protected PicturedItemPickerControl(ItemPicker itemPicker, List<T> items)
        : base(itemPicker)
    {
        Selection.Subscribe(UpdateImageElement);
        Items = items;
    }

    public virtual void UpdateImageElement(T item)
    {
        ItemPicker.ItemLabel.text = "";
        ItemPicker.ItemLabel.style.backgroundImage = GetBackgroundImageValue(item);
    }

    protected abstract StyleBackground GetBackgroundImageValue(T item);
}
