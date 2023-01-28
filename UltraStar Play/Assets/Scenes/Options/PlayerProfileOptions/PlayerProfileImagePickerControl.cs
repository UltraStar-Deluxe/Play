using UnityEngine;
using UnityEngine.UIElements;

public class PlayerProfileImagePickerControl : PicturedItemPickerControl<string>
{
    private readonly UiManager uiManager;

    public PlayerProfileImagePickerControl(ItemPicker itemPicker, UiManager uiManager)
        : base(itemPicker, uiManager.GetRelativePlayerProfileImagePaths())
    {
        this.uiManager = uiManager;

        // The initial value might have be set in the base constructor, where the uiManager was not defined yet.
        // The the image must be updated now.
        UpdateImageElement(SelectedItem);
    }

    public override void UpdateImageElement(string imagePath)
    {
        base.UpdateImageElement(imagePath);

        if (uiManager == null)
        {
            return;
        }

        uiManager.LoadPlayerProfileImage(imagePath, loadedSprite =>
        {
            ItemPicker.ItemLabel.style.backgroundImage = new StyleBackground(loadedSprite);
        });
    }

    protected override StyleBackground GetBackgroundImageValue(string item)
    {
        return new StyleBackground();
    }
}
