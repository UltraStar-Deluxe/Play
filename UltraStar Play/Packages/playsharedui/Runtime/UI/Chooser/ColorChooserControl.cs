using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class ColorChooserControl : PicturedChooserControl<Color32>
{
    public ColorChooserControl(Chooser chooser, List<Color32> items)
        : base(chooser, items)
    {
    }

    public override void UpdateImageElement(Color32 item)
    {
        Chooser.ItemLabel.SetTranslatedText(Translation.Empty);
        Chooser.ItemLabel.style.backgroundColor = new StyleColor(item);
    }

    protected override StyleBackground GetBackgroundImageValue(Color32 item)
    {
        return new StyleBackground();
    }
}
