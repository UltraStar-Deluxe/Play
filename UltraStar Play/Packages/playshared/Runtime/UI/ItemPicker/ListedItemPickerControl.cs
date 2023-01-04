using System.Collections.Generic;

public abstract class ListedItemPickerControl<T> : AbstractItemPickerControl<T>
{
    public bool WrapAround => ItemPicker.WrapAround
        || ItemPicker.NoPreviousButton
        || ItemPicker.NoNextButton;

    protected ListedItemPickerControl(ItemPicker itemPicker)
        : base(itemPicker)
    {
    }

    private List<T> items = new();
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

    public override void SelectPreviousItem()
    {
        if (Items.IsNullOrEmpty())
        {
            return;
        }

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

    public override void SelectNextItem()
    {
        if (Items.IsNullOrEmpty())
        {
            return;
        }

        if (HasSelectedItem)
        {
            if (WrapAround
                || SelectedItemIndex < Items.Count - 1)
            {
                Selection.Value = Items.GetElementAfter(SelectedItem, WrapAround);
            }
        }
        else
        {
            Selection.Value = Items[0];
        }
    }

    public bool TrySelectItem(T item, bool selectFirstItemAsFallback = true)
    {
        if (Items.IsNullOrEmpty())
        {
            return false;
        }

        int index = Items.IndexOf(item);
        if (index >= 0)
        {
            Selection.Value = Items[index];
            return true;
        }

        if (selectFirstItemAsFallback)
        {
            Selection.Value = Items[0];
        }
        return false;
    }
}
