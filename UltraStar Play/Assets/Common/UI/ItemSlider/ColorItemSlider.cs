using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class ColorItemSlider : ItemSlider<Color32>
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
