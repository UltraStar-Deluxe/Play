using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerProfileImagePickerControl : PicturedItemPickerControl<string>
{
    private readonly int playerProfileIndex;
    private readonly UiManager uiManager;
    private readonly WebCamManager webCamManager;
    private readonly Button takeWebCamImageButton;
    private readonly Button removeWebCamImageButton;

    public PlayerProfileImagePickerControl(
        ItemPicker itemPicker,
        int playerProfileIndex,
        UiManager uiManager,
        WebCamManager webCamManager)
        : base(itemPicker, uiManager.GetRelativePlayerProfileImagePaths(false)
            .Union(new List<string> { PlayerProfile.WebcamImagePath }).ToList())
    {
        this.playerProfileIndex = playerProfileIndex;
        this.uiManager = uiManager;
        this.webCamManager = webCamManager;

        takeWebCamImageButton = ItemPicker.Q<Button>(R.UxmlNames.takeWebCamImageButton);
        removeWebCamImageButton = ItemPicker.Q<Button>(R.UxmlNames.removeWebCamImageButton);

        takeWebCamImageButton.RegisterCallbackButtonTriggered(_ => TakeWebCamImage());
        removeWebCamImageButton.RegisterCallbackButtonTriggered(_ => RemoveWebCamImage());

        // The initial value might have be set in the base constructor, where the uiManager was not defined yet.
        // The the image must be updated now.
        UpdateImageElement(SelectedItem);
    }

    public override void UpdateImageElement(string imagePath)
    {
        base.UpdateImageElement(imagePath);
        ItemPicker.ItemImage.image = null;

        if (uiManager == null)
        {
            return;
        }

        if (imagePath == PlayerProfile.WebcamImagePath
            && webCamManager != null)
        {
            string webCamImagePath = GetWebCamImagePath();
            if (File.Exists(webCamImagePath))
            {
                takeWebCamImageButton.HideByDisplay();
                removeWebCamImageButton.ShowByDisplay();

                uiManager.LoadPlayerProfileImage(webCamImagePath, loadedSprite =>
                {
                    ItemPicker.ItemLabel.style.backgroundImage = new StyleBackground(loadedSprite);
                });
            }
            else
            {
                takeWebCamImageButton.ShowByDisplay();
                removeWebCamImageButton.HideByDisplay();

                WebCamTexture webCamTexture = webCamManager.StartSelectedWebCam();
                ItemPicker.ItemImage.image = webCamTexture;
            }
            return;
        }

        takeWebCamImageButton.HideByDisplay();
        removeWebCamImageButton.HideByDisplay();
        uiManager.LoadPlayerProfileImage(imagePath, loadedSprite =>
        {
            ItemPicker.ItemLabel.style.backgroundImage = new StyleBackground(loadedSprite);
        });
    }

    protected override StyleBackground GetBackgroundImageValue(string item)
    {
        return new StyleBackground();
    }

    private void TakeWebCamImage()
    {
        string webCamImagePath = GetWebCamImagePath();
        webCamManager.SaveSnapshot(webCamImagePath);
        uiManager.UpdatePlayerProfileImagePaths();
        ImageManager.RemoveUnusedSpritesFromCache();
        UpdateImageElement(SelectedItem);
    }

    private void RemoveWebCamImage()
    {
        string webCamImagePath = GetWebCamImagePath();
        if (File.Exists(webCamImagePath))
        {
            File.Delete(webCamImagePath);
        }
        UpdateImageElement(SelectedItem);
    }

    private string GetWebCamImagePath()
    {
        return PlayerProfileUtils.GetAbsoluteWebCamImagePath(playerProfileIndex);
    }
}
