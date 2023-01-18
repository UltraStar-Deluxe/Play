using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AvatarImageControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Optional = true)]
    private PlayerProfile playerProfile;
    public PlayerProfile PlayerProfile
    {
        get
        {
            return playerProfile;
        }
        set
        {
            playerProfile = value;
            UpdatePlayerImage();
        }
    }

    [Inject(Optional = true)]
    private MicProfile micProfile;
    public MicProfile MicProfile
    {
        get
        {
            return micProfile;
        }
        set
        {
            micProfile = value;
            UpdatePlayerImage();
        }
    }

    [Inject]
    private UiManager uiManager;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement image;

    public void OnInjectionFinished()
    {
        UpdatePlayerImage();
    }

    private void UpdatePlayerImage()
    {
        if (playerProfile == null)
        {
            return;
        }

        Sprite avatarSprite = uiManager.GetAvatarSprite(playerProfile.Avatar);
        if (avatarSprite == null)
        {
            Debug.LogWarning("Did not find an image for the avatar: " + playerProfile.Avatar);
            return;
        }

        if (micProfile != null)
        {
            image.style.unityBackgroundImageTintColor = new StyleColor(micProfile.Color);
            image.style.backgroundImage = new StyleBackground(avatarSprite);
        }
    }
}
