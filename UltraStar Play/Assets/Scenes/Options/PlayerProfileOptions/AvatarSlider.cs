using System.Linq;
using UnityEngine;

public class AvatarSlider : ImageItemSlider<EAvatar>
{
    AvatarImageReference[] imageReferences;

    void OnEnable()
    {
        imageReferences = FindObjectsOfType<AvatarImageReference>();
        Items = EnumUtils.GetValuesAsList<EAvatar>();
    }

    protected override Sprite GetDisplaySprite(EAvatar value)
    {
        AvatarImageReference imageReference = imageReferences.Where(it => it.avatar == value).FirstOrDefault();
        if (imageReference != null)
        {
            return imageReference.Sprite;
        }
        else
        {
            return null;
        }
    }
}
