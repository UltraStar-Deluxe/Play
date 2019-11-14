using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

public class ColorItemSlider : ItemSlider<Color>
{
    public Image uiImage;

    protected override void Awake()
    {
        base.Awake();
        if (uiImage == null)
        {
            GetComponentInChildren<ItemSliderUiImage>().IfNotNull(it => uiImage = it.GetComponent<Image>());
        }
    }

    protected override void Start()
    {
        base.Start();
        Selection.Subscribe(newValue => uiImage.color = newValue);
    }
}
