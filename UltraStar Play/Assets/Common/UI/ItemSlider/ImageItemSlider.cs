using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

abstract public class ImageItemSlider<T> : ItemSlider<T>
{
    public Image uiItemImage;

    protected override void Start()
    {
        base.Start();
        Selection.Subscribe(newValue => uiItemImage.sprite = GetDisplaySprite(newValue));
    }

    abstract protected Sprite GetDisplaySprite(T value);
}
