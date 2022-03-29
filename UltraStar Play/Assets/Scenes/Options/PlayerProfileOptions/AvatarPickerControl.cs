using UnityEngine;
using UnityEngine.UIElements;

public class AvatarPickerControl : PicturedItemPickerControl<EAvatar>
{
    private readonly UiManager uiManager;

    public AvatarPickerControl(ItemPicker itemPicker, UiManager uiManager)
        : base(itemPicker, EnumUtils.GetValuesAsList<EAvatar>())
    {
        Items = EnumUtils.GetValuesAsList<EAvatar>();
        this.uiManager = uiManager;
    }

    protected override StyleBackground GetBackgroundImageValue(EAvatar item)
    {
        Sprite avatarSprite = uiManager.GetAvatarSprite(item);
        if (avatarSprite == null)
        {
            return new StyleBackground();
        }
        return new StyleBackground(avatarSprite);
    }
}
