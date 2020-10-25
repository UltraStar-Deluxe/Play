// https://github.com/yasirkula/UnityAndroidStreamingAssets
// License: MIT

// On Android the StreamingAssets are packed in a JAR, together with all other resources.
// This script extracts (via SharpZipLib) the StreamingAssets from the JAR to a normal file system location.
// From there the StreamingAssets can be loaded synchronously like on other platforms (via System.IO classes).
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;

public static class AndroidStreamingAssets
{
    private const string STREAMING_ASSETS_DIR = "assets/";
    private const string STREAMING_ASSETS_INTERNAL_DATA_DIR = "assets/bin/";
    private const string META_EXTENSION = ".meta";

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
        string targetPath = Application.temporaryCachePath;
        string result = System.IO.Path.Combine(Application.temporaryCachePath, "assets");

        if (Directory.Exists(result))
        {
            Directory.Delete(result, true);
        }

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
                    if (name.StartsWith(STREAMING_ASSETS_DIR)
                        && !name.EndsWith(META_EXTENSION)
                        && !name.StartsWith(STREAMING_ASSETS_INTERNAL_DATA_DIR))
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

        path = result;
#else
        path = Application.streamingAssetsPath;
#endif
    }
}
