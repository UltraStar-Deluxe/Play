using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;

public class PlayerProfileImageManager : AbstractSingletonBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        relativePlayerProfileImagePathToAbsolutePath = new();
    }

    public static PlayerProfileImageManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<PlayerProfileImageManager>();

    private static Dictionary<string, string> relativePlayerProfileImagePathToAbsolutePath = new();

    [InjectedInInspector]
    public Sprite fallbackPlayerProfileImage;

    [Inject]
    private Settings settings;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        UpdatePlayerProfileImagePaths();
    }

    public void ReloadPlayerProfileImages()
    {
        UpdatePlayerProfileImagePaths();
    }

    public void UpdatePlayerProfileImagePaths()
    {
        List<string> folders = PlayerProfileUtils.GetPlayerProfileImageFolders();
        relativePlayerProfileImagePathToAbsolutePath = PlayerProfileUtils.FindPlayerProfileImages(folders);
    }


    public string GetFinalPlayerProfileImagePath(PlayerProfile playerProfile)
    {
        if (playerProfile.ImagePath == PlayerProfile.WebcamImagePath)
        {
            int playerProfileIndex = settings.PlayerProfiles.IndexOf(playerProfile);
            string webCamImagePath = PlayerProfileUtils.GetAbsoluteWebCamImagePath(playerProfileIndex);
            return webCamImagePath;
        }
        else
        {
            return playerProfile.ImagePath;
        }
    }

    public async Awaitable<Sprite> LoadPlayerProfileImageAsync(string imagePath)
    {
        if (imagePath.IsNullOrEmpty())
        {
            return fallbackPlayerProfileImage;
        }

        string relativePathNormalized = PathUtils.NormalizePath(imagePath);
        string matchingFullPath = GetAbsolutePlayerProfileImagePaths().FirstOrDefault(absolutePath =>
        {
            string absolutePathNormalized = PathUtils.NormalizePath(absolutePath);
            return absolutePathNormalized.EndsWith(relativePathNormalized);
        });

        if (matchingFullPath.IsNullOrEmpty())
        {
            Debug.LogWarning($"Cannot load player profile image with path '{imagePath}' (normalized: '{relativePathNormalized}'), no corresponding image file found.");
            return fallbackPlayerProfileImage;
        }

        return await ImageManager.LoadSpriteFromUriAsync(matchingFullPath);
    }

    public List<string> GetAbsolutePlayerProfileImagePaths()
    {
        return relativePlayerProfileImagePathToAbsolutePath.Values.ToList();
    }

    public List<string> GetRelativePlayerProfileImagePaths(bool includeWebCamImages)
    {
        if (includeWebCamImages)
        {
            return relativePlayerProfileImagePathToAbsolutePath.Keys.ToList();
        }
        else
        {
            return relativePlayerProfileImagePathToAbsolutePath.Keys
                .Where(relativePath => !relativePath.Contains(PlayerProfileUtils.PlayerProfileWebCamImagesFolderName))
                .ToList();
        }
    }
}
