using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

abstract public class ImageItemSlider<T> : ItemSlider<T>
{
    public Image uiItemImage;

    protected override void Awake()
    {
        base.Awake();
        if (uiItemImage == null)
        {
            GetComponentInChildren<ItemSliderUiImage>().IfNotNull(it => uiItemImage = it.GetComponent<Image>());
        }
    }

    protected override void Start()
    {
        base.Start();
        Selection.Subscribe(newValue => uiItemImage.sprite = GetDisplaySprite(newValue));
    }

    abstract protected Sprite GetDisplaySprite(T value);
}
