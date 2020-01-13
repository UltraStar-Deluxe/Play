using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

abstract public class ItemSlider<T> : MonoBehaviour
{
    public bool wrapAround;
    public Button previousItemButton;
    public Button nextItemButton;

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

    protected virtual void Awake()
    {
        if (previousItemButton == null)
        {
            GetComponentInChildren<ItemSliderPreviousItemButton>().IfNotNull(it => previousItemButton = it.GetComponent<Button>());
        }
        if (nextItemButton == null)
        {
            GetComponentInChildren<ItemSliderNextItemButton>().IfNotNull(it => nextItemButton = it.GetComponent<Button>());
        }
    }

    protected virtual void Start()
    {
        if (previousItemButton != null)
        {
            previousItemButton.OnClickAsObservable().Subscribe(_ => SelectPreviousItem());
        }
        if (nextItemButton != null)
        {
            nextItemButton.OnClickAsObservable().Subscribe(_ => SelectNextItem());
        }
    }

    public void SelectPreviousItem()
    {
        if (HasSelectedItem)
        {
            if (wrapAround || SelectedItemIndex > 0)
            {
                Selection.Value = Items.GetElementBefore(SelectedItem, wrapAround);
            }
        }
        else
        {
            Selection.Value = Items[0];
            return;
        }
    }

    public void SelectNextItem()
    {
        if (HasSelectedItem)
        {
            if (wrapAround || SelectedItemIndex < Items.Count - 1)
            {
                Selection.Value = Items.GetElementAfter(SelectedItem, wrapAround);
            }
        }
        else
        {
            Selection.Value = Items[0];
            return;
        }
    }
}
