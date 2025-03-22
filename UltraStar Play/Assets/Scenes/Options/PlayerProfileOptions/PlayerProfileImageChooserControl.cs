using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerProfileImageChooserControl : PicturedChooserControl<string>
{
    private readonly int playerProfileIndex;
    private readonly PlayerProfileImageManager playerProfileImageManager;
    private readonly WebCamManager webCamManager;
    private readonly Button takeWebCamImageButton;
    private readonly Button removeWebCamImageButton;

    public PlayerProfileImageChooserControl(
        Chooser chooser,
        int playerProfileIndex,
        PlayerProfileImageManager playerProfileImageManager,
        WebCamManager webCamManager)
        : base(chooser, playerProfileImageManager.GetRelativePlayerProfileImagePaths(false)
            .Union(new List<string> { PlayerProfile.WebcamImagePath }).ToList())
    {
        this.playerProfileIndex = playerProfileIndex;
        this.playerProfileImageManager = playerProfileImageManager;
        this.webCamManager = webCamManager;

        takeWebCamImageButton = Chooser.Q<Button>(R.UxmlNames.takeWebCamImageButton);
        removeWebCamImageButton = Chooser.Q<Button>(R.UxmlNames.removeWebCamImageButton);

        takeWebCamImageButton.RegisterCallbackButtonTriggered(_ => TakeWebCamImage());
        removeWebCamImageButton.RegisterCallbackButtonTriggered(_ => RemoveWebCamImage());

        // The initial value might have be set in the base constructor, where the uiManager was not defined yet.
        // The the image must be updated now.
        UpdateImageElement(Selection);
    }

    public override async void UpdateImageElement(string imagePath)
    {
        base.UpdateImageElement(imagePath);
        Chooser.ItemImage.image = null;

        if (playerProfileImageManager == null)
        {
            return;
        }

        Chooser.ItemLabel.SetBorderRadius(Length.Percent(50));

        if (imagePath == PlayerProfile.WebcamImagePath
            && webCamManager != null)
        {
            string webCamImagePath = GetWebCamImagePath();
            if (File.Exists(webCamImagePath))
            {
                takeWebCamImageButton.HideByDisplay();
                removeWebCamImageButton.ShowByDisplay();

                Sprite loadedWebCamImage = await playerProfileImageManager.LoadPlayerProfileImageAsync(webCamImagePath);
                Chooser.ItemLabel.style.backgroundImage = new StyleBackground(loadedWebCamImage);
            }
            else
            {
                takeWebCamImageButton.ShowByDisplay();
                removeWebCamImageButton.HideByDisplay();

                WebCamTexture webCamTexture = webCamManager.StartSelectedWebCam();
                Chooser.ItemImage.image = webCamTexture;
            }
            return;
        }

        takeWebCamImageButton.HideByDisplay();
        removeWebCamImageButton.HideByDisplay();
        Sprite loadedImage = await playerProfileImageManager.LoadPlayerProfileImageAsync(imagePath);
        Chooser.ItemLabel.style.backgroundImage = new StyleBackground(loadedImage);
    }

    protected override StyleBackground GetBackgroundImageValue(string item)
    {
        return new StyleBackground();
    }

    private void TakeWebCamImage()
    {
        string webCamImagePath = GetWebCamImagePath();
        webCamManager.SaveSnapshot(webCamImagePath);
        playerProfileImageManager.UpdatePlayerProfileImagePaths();
        ImageManager.RemoveUnusedSpritesFromCache();
        UpdateImageElement(Selection);
    }

    private void RemoveWebCamImage()
    {
        string webCamImagePath = GetWebCamImagePath();
        if (File.Exists(webCamImagePath))
        {
            File.Delete(webCamImagePath);
        }
        UpdateImageElement(Selection);
    }

    private string GetWebCamImagePath()
    {
        return PlayerProfileUtils.GetAbsoluteWebCamImagePath(playerProfileIndex);
    }
}
