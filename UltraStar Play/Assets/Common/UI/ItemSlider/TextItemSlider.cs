using UnityEngine;
using UnityEngine.UI;
using UniRx;

abstract public class TextItemSlider<T> : ItemSlider<T>
{
    public Text uiItemText;

    protected override void Start()
    {
        base.Start();
        Selection.Subscribe(newValue => uiItemText.text = GetDisplayString(newValue));
    }

    protected virtual string GetDisplayString(T value)
    {
        return value.ToString();
    }
}
