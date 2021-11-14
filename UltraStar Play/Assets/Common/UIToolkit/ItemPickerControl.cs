using System;
using System.Collections.Generic;
using UniRx;

public class ItemPickerControl<T>
{
    public ItemPicker ItemPicker { get; private set; }

    public bool WrapAround => ItemPicker.wrapAround;

    public ItemPickerControl(ItemPicker itemPicker)
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

    private List<T> items = new List<T>();
    public List<T> Items
    {
        get
        {
            return new List<T>(items);
        }
        set
        {
            if (value == null)
            {
                items = new List<T>();
            }
            else
            {
                // Return a copy of the item list, to not mess up the item list from the outside.
                items = new List<T>(value);
            }

            // Remove selection if not in the new items list.
            if (HasSelectedItem && !items.Contains(SelectedItem))
            {
                Selection.Value = default(T);
            }
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

    public virtual bool HasSelectedItem
    {
        get
        {
            // ValueTypes are never null, and thus always selected.
            // Comparing with default(T) is used e.g. for structs, which are never null.
            return typeof(T).IsValueType || !object.Equals(SelectedItem, default(T));
        }
    }

    public int SelectedItemIndex
    {
        get
        {
            return Items.IndexOf(SelectedItem);
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

    public void SelectPreviousItem()
    {
        if (HasSelectedItem)
        {
            if (WrapAround || SelectedItemIndex > 0)
            {
                Selection.Value = Items.GetElementBefore(SelectedItem, WrapAround);
            }
        }
        else
        {
            Selection.Value = Items[0];
        }
    }

    public void SelectNextItem()
    {
        if (HasSelectedItem)
        {
            if (WrapAround || SelectedItemIndex < Items.Count - 1)
            {
                Selection.Value = Items.GetElementAfter(SelectedItem, WrapAround);
            }
        }
        else
        {
            Selection.Value = Items[0];
        }
    }
}
