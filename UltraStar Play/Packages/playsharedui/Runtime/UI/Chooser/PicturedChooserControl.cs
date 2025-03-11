using System.Collections.Generic;
using UniRx;
using UnityEngine.UIElements;

public abstract class PicturedChooserControl<T> : ListedChooserControl<T>
{
    protected PicturedChooserControl(Chooser chooser, List<T> items)
        : base(chooser)
    {
        SelectionAsObservable.Subscribe(UpdateImageElement);
        Items = items;
    }

    public virtual void UpdateImageElement(T item)
    {
        Chooser.ItemLabel.text = "";
        Chooser.ItemLabel.style.backgroundImage = GetBackgroundImageValue(item);
    }

    protected abstract StyleBackground GetBackgroundImageValue(T item);
}
