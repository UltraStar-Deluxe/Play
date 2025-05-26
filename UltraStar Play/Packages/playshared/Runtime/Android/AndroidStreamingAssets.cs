// https://github.com/yasirkula/UnityAndroidStreamingAssets
// License: MIT

// On Android the StreamingAssets are packed in a JAR, together with all other resources.
// This script extracts (via SharpZipLib) the StreamingAssets from the JAR to a normal file system location.
// From there the StreamingAssets can be loaded synchronously like on other platforms (via System.IO classes).

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using UnityEngine;

public static class AndroidStreamingAssets
{
    private const string StreamingAssetsDir = "assets/";
    private const string StreamingAssetsInternalDataDir = "assets/bin/";
    private const string MetaExtension = ".meta";
    private const string AssetsHashFileName = "AssetsHash.txt";

    private static string path;
    public static string Path
    {
        get
        {
            if (path == null)
            {
                Extract();
            }

            return path;
        }
    }

    public static void Extract()
    {
#if !UNITY_EDITOR && UNITY_ANDROID
        string targetPath = $"{Application.persistentDataPath}/ExtractedStreamingAssets";
        path = $"{targetPath}/assets";

        string assetsHashFilePath = $"{Application.persistentDataPath}/{AssetsHashFileName}";
        string oldAssetsHash = GetAssetsHash(assetsHashFilePath);
        string newAssetsHash = ComputeAssetsHash(Application.dataPath);

        if (Directory.Exists(path))
        {
            if (oldAssetsHash == newAssetsHash)
            {
                Debug.Log($"Found existing extracted StreamingAssets folder with matching assets hash {newAssetsHash}");
                return;
            }
            Debug.Log($"Found existing extracted StreamingAssets folder but assets hash changed to '{newAssetsHash}' from '{oldAssetsHash}'");
            Directory.Delete(path, true);
        }

        using DisposableStopwatch disposableStopwatch = new($"Extracting StreamingAssets folder took <ms> ms");
        Debug.Log($"Extracting StreamingAssets folder to {targetPath}");
        Directory.CreateDirectory(targetPath);

        if (targetPath[targetPath.Length - 1] != '/' || targetPath[targetPath.Length - 1] != '\\')
        {
            targetPath += '/';
        }

        HashSet<string> createdDirectories = new HashSet<string>();

        ZipFile zf = null;
        try
        {
            using (FileStream fs = File.OpenRead(Application.dataPath))
            {
                zf = new ZipFile(fs);
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;
                    }

                    string name = zipEntry.Name;
                    if (name.StartsWith(StreamingAssetsDir)
                        && !name.EndsWith(MetaExtension)
                        && !name.StartsWith(StreamingAssetsInternalDataDir))
                    {
                        string relativeDir = System.IO.Path.GetDirectoryName(name);
                        if (!createdDirectories.Contains(relativeDir))
                        {
                            Directory.CreateDirectory(targetPath + relativeDir);
                            createdDirectories.Add(relativeDir);
                        }

                        byte[] buffer = new byte[4096];
                        using (Stream zipStream = zf.GetInputStream(zipEntry))
                        using (FileStream streamWriter = File.Create(targetPath + name))
                        {
                            StreamUtils.Copy(zipStream, streamWriter, buffer);
                        }
                    }
                }
            }
        }
        finally
        {
            if (zf != null)
            {
                zf.IsStreamOwner = true;
                zf.Close();
            }
        }

        SetAssetsHash(assetsHashFilePath, newAssetsHash);
        Debug.Log("Extracting StreamingAssets folder done");
#else
        path = Application.streamingAssetsPath;
#endif
    }

    private static string GetAssetsHash(string hashFilePath)
    {
        if (!File.Exists(hashFilePath))
        {
            return "";
        }
        return File.ReadAllText(hashFilePath);
    }

    private static void SetAssetsHash(string hashFilePath, string hash)
    {
        string folderPath = System.IO.Path.GetDirectoryName(hashFilePath);
        if (folderPath.IsNullOrEmpty())
        {
            return;
        }

        Directory.CreateDirectory(folderPath);
        File.WriteAllText(hashFilePath, hash);
    }

    private static string ComputeAssetsHash(string assetsFilePath)
    {
        using DisposableStopwatch disposableStopwatch = new($"{nameof(ComputeAssetsHash)} for {assetsFilePath} took <ms> ms");
        // Simply use the last write time as hash
        DateTime lastWriteTimeUtc = new FileInfo(assetsFilePath).LastWriteTimeUtc;
        string lastWriteTimeUtcString = lastWriteTimeUtc.ToString("yyyy-MM-dd'T'HH:mm:ss.fffK", CultureInfo.InvariantCulture);
        return lastWriteTimeUtcString;
    }
}
