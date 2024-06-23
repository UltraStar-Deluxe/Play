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

    private readonly IReactiveProperty<T> selectionProperty = new ReactiveProperty<T>();
    public IObservable<T> SelectionAsObservable => selectionProperty;

    public T Selection
    {
        get => selectionProperty.Value;
        set
        {
            if (Equals(Selection, value))
            {
                return;
            }
            selectionProperty.Value = value;
        }
    }

    public void Bind(Func<T> getter, Action<T> setter)
    {
        Selection = getter.Invoke();
        SelectionAsObservable.Subscribe(newValue => setter.Invoke(newValue));
    }

    public abstract void SelectPreviousItem();

    public abstract void SelectNextItem();
}
