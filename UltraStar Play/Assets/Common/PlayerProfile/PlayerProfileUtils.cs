using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class PlayerProfileUtils
{
    public const string PlayerProfileImagesFolderName = "PlayerProfileImages";
    public const string PlayerProfileWebCamImagesFolderName = "WebcamImages";
    public static List<string> AdditionalPlayerProfileImageFolders { get; set; } = new();

    public static List<string> GetPlayerProfileImageFolders()
    {
        return new List<string>
            {
                PlayerProfileUtils.GetDefaultPlayerProfileImageFolderAbsolutePath(),
                PlayerProfileUtils.GetUserDefinedPlayerProfileImageFolderAbsolutePath(),
            }
            .Union(AdditionalPlayerProfileImageFolders)
            .ToList();
    }

    public static string GetAbsoluteWebCamImageFolder()
    {
        return $"{GetDefaultPlayerProfileImageFolderAbsolutePath()}/WebcamImages";
    }

    public static string GetAbsoluteWebCamImagePath(int playerProfileIndex)
    {
        return $"{GetAbsoluteWebCamImageFolder()}/Player-{playerProfileIndex}.png";
    }

    public static string GetDefaultPlayerProfileImageFolderAbsolutePath()
    {
        return ApplicationUtils.GetStreamingAssetsPath(PlayerProfileImagesFolderName);
    }

    public static string GetUserDefinedPlayerProfileImageFolderAbsolutePath()
    {
        return ApplicationUtils.GetPersistentDataPath(PlayerProfileImagesFolderName);
    }

    public static Dictionary<string, string> FindPlayerProfileImages(List<string> folders)
    {
        Dictionary<string, string> result = new();
        folders.ForEach(folder =>
        {
            if (Directory.Exists(folder))
            {
                string[] pngFilesInFolder = Directory.GetFiles(folder, "*.png", SearchOption.AllDirectories);
                string[] jpgFilesInFolder = Directory.GetFiles(folder, "*.jpg", SearchOption.AllDirectories);
                List<string> imageFilesInFolder = pngFilesInFolder
                    .Union(jpgFilesInFolder)
                    .ToList();
                imageFilesInFolder.ForEach(absolutePath =>
                {
                    string relativePath = absolutePath.Substring(folder.Length + 1);
                    result.Add(relativePath, absolutePath);
                });
            }
        });

        Debug.Log($"Found {result.Count} player profile images: {JsonConverter.ToJson(result)}");

        return result;
    }
}
