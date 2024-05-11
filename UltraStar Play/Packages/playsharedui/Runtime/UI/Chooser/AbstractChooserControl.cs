using System;
using UniRx;

public abstract class AbstractChooserControl<T>
{
    public Chooser Chooser { get; private set; }

    protected AbstractChooserControl(Chooser chooser)
    {
        chooser.InitControl(this);
        this.Chooser = chooser;
        if (chooser.PreviousItemButton != null)
        {
            chooser.PreviousItemButton.RegisterCallbackButtonTriggered(_ => SelectPreviousItem());
        }
        if (chooser.NextItemButton != null)
        {
            chooser.NextItemButton.RegisterCallbackButtonTriggered(_ => SelectNextItem());
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
        if (Equals(SelectedItem, item))
        {
            return;
        }
        Selection.Value = item;
    }

    public abstract void SelectPreviousItem();

    public abstract void SelectNextItem();
}
