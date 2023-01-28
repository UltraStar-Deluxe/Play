using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public static class PlayerProfileUtils
{
    public const string PlayerProfileImagesFolderName = "PlayerProfileImages";
    public const string PlayerProfileWebCamImagesFolderName = "WebcamImages";

    public static string GetAbsoluteWebCamImageFolder()
    {
        return $"{Application.persistentDataPath}/{PlayerProfileImagesFolderName}/WebcamImages";
    }

    public static string GetAbsoluteWebCamImagePath(int playerProfileIndex)
    {
        return $"{GetAbsoluteWebCamImageFolder()}/Player-{playerProfileIndex}.png";
    }

    public static Dictionary<string, string> FindPlayerProfileImages()
    {
        List<string> folders = new List<string>
        {
            ApplicationUtils.GetStreamingAssetsPath(PlayerProfileImagesFolderName),
            $"{Application.persistentDataPath}/{PlayerProfileImagesFolderName}",
        };

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
