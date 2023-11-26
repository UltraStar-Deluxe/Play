using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PlayerProfileImageControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Optional = true)]
    private MicProfile micProfile;
    public MicProfile MicProfile
    {
        get => micProfile;
        set
        {
            micProfile = value;
            UpdatePlayerProfileImage();
        }
    }

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
            UpdatePlayerProfileImage();
        }
    }

    [Inject]
    private UiManager uiManager;

    [Inject]
    private Settings settings;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    private VisualElement image;

    public void OnInjectionFinished()
    {
        UpdatePlayerProfileImage();
    }

    private void UpdatePlayerProfileImage()
    {
        if (playerProfile == null)
        {
            return;
        }

        UpdatePlayerImageColors();

        string finalImagePath = uiManager.GetFinalPlayerProfileImagePath(playerProfile);
        uiManager.LoadPlayerProfileImage(finalImagePath)
            .Subscribe(loadedSprite => image.style.backgroundImage = new StyleBackground(loadedSprite));
    }

    private void UpdatePlayerImageColors()
    {
        UpdatePlayerImageBackgroundColor();
        UpdatePlayerImageTintColor();
    }

    private void UpdatePlayerImageTintColor()
    {
        if (playerProfile is LobbyMemberPlayerProfile lobbyMemberPlayerProfile)
        {
             image.style.unityBackgroundImageTintColor = new StyleColor(ColorGenerationUtils.FromString(lobbyMemberPlayerProfile.Name));
        }
    }

    private void UpdatePlayerImageBackgroundColor()
    {
        if (micProfile != null)
        {
            image.style.backgroundColor = new StyleColor(micProfile.Color);
        }

        image.style.backgroundColor =  new StyleColor(Color.clear);
    }
}
