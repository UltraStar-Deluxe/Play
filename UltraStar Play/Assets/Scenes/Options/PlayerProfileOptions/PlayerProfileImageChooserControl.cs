using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerProfileImageChooserControl : PicturedChooserControl<string>
{
    private readonly int playerProfileIndex;
    private readonly UiManager uiManager;
    private readonly WebCamManager webCamManager;
    private readonly Button takeWebCamImageButton;
    private readonly Button removeWebCamImageButton;

    public PlayerProfileImageChooserControl(
        Chooser chooser,
        int playerProfileIndex,
        UiManager uiManager,
        WebCamManager webCamManager)
        : base(chooser, uiManager.GetRelativePlayerProfileImagePaths(false)
            .Union(new List<string> { PlayerProfile.WebcamImagePath }).ToList())
    {
        this.playerProfileIndex = playerProfileIndex;
        this.uiManager = uiManager;
        this.webCamManager = webCamManager;

        takeWebCamImageButton = Chooser.Q<Button>(R.UxmlNames.takeWebCamImageButton);
        removeWebCamImageButton = Chooser.Q<Button>(R.UxmlNames.removeWebCamImageButton);

        takeWebCamImageButton.RegisterCallbackButtonTriggered(_ => TakeWebCamImage());
        removeWebCamImageButton.RegisterCallbackButtonTriggered(_ => RemoveWebCamImage());

        // The initial value might have be set in the base constructor, where the uiManager was not defined yet.
        // The the image must be updated now.
        UpdateImageElement(Selection);
    }

    public override void UpdateImageElement(string imagePath)
    {
        base.UpdateImageElement(imagePath);
        Chooser.ItemImage.image = null;

        if (uiManager == null)
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

                uiManager.LoadPlayerProfileImage(webCamImagePath)
                    .Subscribe(loadedSprite => Chooser.ItemLabel.style.backgroundImage = new StyleBackground(loadedSprite));
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
        uiManager.LoadPlayerProfileImage(imagePath)
            .Subscribe(loadedSprite => Chooser.ItemLabel.style.backgroundImage = new StyleBackground(loadedSprite));
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
