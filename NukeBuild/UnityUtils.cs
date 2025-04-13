using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Nuke.Common.IO;

namespace DefaultNamespace;

public static class UnityUtils
{
    public static string GetUnityVersion(AbsolutePath unityProjectDir)
    {
        string versionPropertyName = "m_EditorVersion";

        return File.ReadAllLines(unityProjectDir / "ProjectSettings" / "ProjectVersion.txt")
            .Where(line => line.StartsWith(versionPropertyName))
            .Select(line => line.Replace($"{versionPropertyName}:", "").Trim())
            .First();
    }

    public static string GetUnityExecutable(string unityVersion)
    {
        // See https://docs.unity3d.com/Manual/EditorCommandLineArguments.html
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"C:/Program Files/Unity/Hub/Editor/{unityVersion}/Editor/Unity.exe";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return $"/Applications/Unity/Hub/Editor/{unityVersion}/Unity.app/Contents/MacOS/Unity";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Path for unity-ci Docker images
            return "/opt/unity/Editor/Unity";
        }
        else
        {
            throw new PlatformNotSupportedException("Unsupported operating system.");
        }
    }
}
