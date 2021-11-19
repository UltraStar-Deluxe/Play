using System;
using System.Collections.Generic;
using UniRx;

public abstract class AbstractItemPickerControl<T>
{
    public ItemPicker ItemPicker { get; private set; }

    protected AbstractItemPickerControl(ItemPicker itemPicker)
    {
        itemPicker.InitControl(this);
        this.ItemPicker = itemPicker;
        if (itemPicker.PreviousItemButton != null)
        {
            itemPicker.PreviousItemButton.RegisterCallbackButtonTriggered(() => SelectPreviousItem());
        }
        if (itemPicker.NextItemButton != null)
        {
            itemPicker.NextItemButton.RegisterCallbackButtonTriggered(() => SelectNextItem());
        }
    }

    public IReactiveProperty<T> Selection { get; private set; } = new ReactiveProperty<T>();

    public T SelectedItem
    {
        get
        {
            return Selection.Value;
        }
    }

    public void Bind(Func<T> getter, Action<T> setter)
    {
        Selection.Value = getter.Invoke();
        Selection.Subscribe(newValue => setter.Invoke(newValue));
    }

    public void SelectItem(T item)
    {
        Selection.Value = item;
    }

    public abstract void SelectPreviousItem();

    public abstract void SelectNextItem();
}
