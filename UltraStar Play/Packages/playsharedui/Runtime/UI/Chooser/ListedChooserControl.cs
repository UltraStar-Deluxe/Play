using System.Collections.Generic;
using System.Linq;

public abstract class ListedChooserControl<T> : AbstractChooserControl<T>
{
    public bool WrapAround => Chooser.WrapAround
        || Chooser.NoPreviousButton
        || Chooser.NoNextButton;

    protected ListedChooserControl(Chooser chooser)
        : base(chooser)
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
            if (HasSelection && !items.Contains(Selection))
            {
                Selection = default(T);
            }
        }
    }

    public virtual bool HasSelection
    {
        get
        {
            return Items.Contains(Selection)
                // ValueTypes are never null, and thus always selected.
                // Comparing with default(T) is used e.g. for structs, which are never null.
                || typeof(T).IsValueType || !Equals(Selection, default(T));
        }
    }

    public int SelectionIndex
    {
        get
        {
            return Items.IndexOf(Selection);
        }
    }

    public override void SelectPreviousItem()
    {
        if (Items.IsNullOrEmpty())
        {
            return;
        }

        if (SelectionIndex < 0
            && !Items.IsNullOrEmpty())
        {
            Selection = Items.LastOrDefault();
            return;
        }

        if (HasSelection)
        {
            if (WrapAround || SelectionIndex > 0)
            {
                Selection = Items.GetElementBefore(Selection, WrapAround);
            }
        }
        else
        {
            Selection = Items[0];
        }
    }

    public override void SelectNextItem()
    {
        if (Items.IsNullOrEmpty())
        {
            return;
        }

        if (SelectionIndex < 0
            && !Items.IsNullOrEmpty())
        {
            Selection = Items.FirstOrDefault();
            return;
        }

        if (HasSelection)
        {
            if (WrapAround
                || SelectionIndex < Items.Count - 1)
            {
                Selection = Items.GetElementAfter(Selection, WrapAround);
            }
        }
        else
        {
            Selection = Items[0];
        }
    }

    public bool TrySetSelection(T item, bool selectFirstItemAsFallback = true)
    {
        if (Items.IsNullOrEmpty())
        {
            return false;
        }

        int index = Items.IndexOf(item);
        if (index >= 0)
        {
            Selection = Items[index];
            return true;
        }

        if (selectFirstItemAsFallback)
        {
            Selection = Items[0];
        }
        return false;
    }
}
