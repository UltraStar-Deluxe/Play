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
        image.style.backgroundColor = new StyleColor(GetPlayerImageBackgroundColor());
        image.style.unityBackgroundImageTintColor = new StyleColor(GetPlayerImageTintColor());
    }

    private Color32 GetPlayerImageTintColor()
    {
        if (playerProfile is LobbyMemberPlayerProfile lobbyMemberPlayerProfile)
        {
            return ColorGenerationUtils.FromString(lobbyMemberPlayerProfile.Name);
        }

        return Colors.white;
    }

    private Color32 GetPlayerImageBackgroundColor()
    {
        if (micProfile != null)
        {
            return micProfile.Color;
        }

        return Color.clear;
    }
}
