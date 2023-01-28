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

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement image;

    public void OnInjectionFinished()
    {
        if (playerProfile == null)
        {
            return;
        }

        uiManager.LoadPlayerProfileImage(playerProfile.ImagePath, loadedSprite =>
        {
            image.style.backgroundImage = new StyleBackground(loadedSprite);

            if (micProfile != null)
            {
                image.style.unityBackgroundImageTintColor = new StyleColor(micProfile.Color);
            }
        });
    }
}
