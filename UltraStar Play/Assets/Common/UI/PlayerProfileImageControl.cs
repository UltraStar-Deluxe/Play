using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerProfileImageControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject(Optional = true)]
    private PlayerProfile playerProfile;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Settings settings;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement image;

    public void OnInjectionFinished()
    {
        if (playerProfile == null)
        {
            return;
        }

        if (playerProfile.ImagePath == PlayerProfile.WebcamImagePath)
        {
            int playerProfileIndex = settings.PlayerProfiles.IndexOf(playerProfile);
            string webCamImagePath = PlayerProfileUtils.GetAbsoluteWebCamImagePath(playerProfileIndex);
            uiManager.LoadPlayerProfileImage(webCamImagePath, loadedSprite =>
            {
                image.style.backgroundImage = new StyleBackground(loadedSprite);
            });
            return;
        }

        uiManager.LoadPlayerProfileImage(playerProfile.ImagePath, loadedSprite =>
        {
            image.style.backgroundImage = new StyleBackground(loadedSprite);
        });
    }
}
