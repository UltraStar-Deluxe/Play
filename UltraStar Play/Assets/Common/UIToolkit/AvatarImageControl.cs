using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UnityEngine.UIElements;
using Background = UnityEngine.UIElements.Background;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class AvatarImageControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject(Optional = true)]
    private PlayerProfile playerProfile;

    [Inject]
    private UiManager uiManager;

    private readonly VisualElement image;

    public AvatarImageControl(VisualElement image)
    {
        this.image = image;
    }

    public void OnInjectionFinished()
    {
        if (playerProfile == null)
        {
            return;
        }

        AvatarImageReference imageRef = GameObject.FindObjectsOfType<AvatarImageReference>()
                .FirstOrDefault(it => it.avatar == playerProfile.Avatar);
        if (imageRef == null)
        {
            Debug.LogWarning("Did not find an image for the avatar: " + playerProfile.Avatar);
            return;
        }

        if (micProfile != null)
        {
            image.style.unityBackgroundImageTintColor = new StyleColor(micProfile.Color);
            image.style.backgroundImage = new StyleBackground(imageRef.Sprite);
        }
    }
}
