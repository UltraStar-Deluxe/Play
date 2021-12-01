using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class AvatarPickerControl : PicturedItemPickerControl<EAvatar>
{
    private AvatarImageReference[] imageReferences;

    public AvatarPickerControl(ItemPicker itemPicker)
        : base(itemPicker, EnumUtils.GetValuesAsList<EAvatar>())
    {
        Items = EnumUtils.GetValuesAsList<EAvatar>();
    }

    protected override StyleBackground GetBackgroundImageValue(EAvatar item)
    {
        if (imageReferences == null)
        {
            imageReferences = GameObject.FindObjectsOfType<AvatarImageReference>();
            if (imageReferences == null)
            {
                return new StyleBackground();
            }
        }

        AvatarImageReference imageReference = imageReferences
            .FirstOrDefault(it => it.avatar == item);
        if (imageReference != null)
        {
            return new StyleBackground(imageReference.Sprite);
        }
        else
        {
            return new StyleBackground();
        }
    }
}
