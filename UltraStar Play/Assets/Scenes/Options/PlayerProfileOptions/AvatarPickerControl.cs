using UnityEngine;
using UnityEngine.UIElements;

public class AvatarPickerControl : PicturedItemPickerControl<EAvatar>
{
    private readonly UiManager uiManager;

    public AvatarPickerControl(ItemPicker itemPicker, UiManager uiManager)
        : base(itemPicker, EnumUtils.GetValuesAsList<EAvatar>())
    {
        this.uiManager = uiManager;

        // The initial value might have be set in the base constructor, where the uiManager was not defined yet.
        // The the image must be updated now.
        UpdateImageElement(SelectedItem);
    }

    protected override StyleBackground GetBackgroundImageValue(EAvatar item)
    {
        if (uiManager == null)
        {
            return new StyleBackground();
        }

        Sprite avatarSprite = uiManager.GetAvatarSprite(item);
        if (avatarSprite == null)
        {
            return new StyleBackground();
        }
        return new StyleBackground(avatarSprite);
    }
}
