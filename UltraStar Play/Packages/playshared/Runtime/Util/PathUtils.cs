using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class PathUtils
{
    public static string CombinePaths(string firstPart, string secondPart)
    {
        bool EndsWithSeparator(string path)
        {
            return path.EndsWith("/") || path.EndsWith("\\");
        }

        bool StartsWithSeparator(string path)
        {
            return path.StartsWith("/") || path.StartsWith("\\");
        }

        if (EndsWithSeparator(firstPart) || StartsWithSeparator(secondPart))
        {
            return firstPart + secondPart;
        }

        return firstPart + $"/{secondPart}";
    }

    // See https://stackoverflow.com/questions/60365892/how-to-determine-if-a-path-is-fully-qualified
    public static bool IsAbsolutePath(string path)
    {
        if (path == null)
        {
            return false;
        }

        if (path.StartsWith("/"))
        {
            // Root folder.
            return true;
        }

        if (path.Length < 2)
        {
            // There is no other way to specify an absolute path with a single character
            return false;
        }

        if (path.Length == 2
            && IsValidDriveChar(path[0])
            && path[1] == System.IO.Path.VolumeSeparatorChar)
        {
            // 'C:' or similar
            return true;
        }

        if (path.Length >= 3
            && IsValidDriveChar(path[0])
            && path[1] == System.IO.Path.VolumeSeparatorChar
            && IsDirectorySeparator(path[2]))
        {
            // 'C:\' or similar
            return true;
        }

        if (path.Length >= 3
            && IsDirectorySeparator(path[0])
            && IsDirectorySeparator(path[1]))
        {
            // This is start of a UNC path, e.g. '\\SOME-HOST\SharedFolder'
            return true;
        }

        return false;
    }

    private static bool IsDirectorySeparator(char c)
    {
        return c == Path.DirectorySeparatorChar
               || c == Path.AltDirectorySeparatorChar;
    }

    private static bool IsValidDriveChar(char c)
    {
        return c >= 'A' && c <= 'Z'
               || c >= 'a' && c <= 'z';
    }

    public static string NormalizePath(string path)
    {
        if (IsAbsolutePath(path))
        {
            return Path.GetFullPath(path).Replace("\\", "/");
        }

        // https://stackoverflow.com/questions/37361309/normalize-a-relative-path
        string dummyBasePath = "c:/dummy-base-path/";
        Uri baseUri = new Uri(dummyBasePath);
        if (Uri.TryCreate(baseUri, path, out Uri normalizedUri))
        {
            if (normalizedUri.AbsolutePath.StartsWith(dummyBasePath))
            {
                return normalizedUri.AbsolutePath.Substring(dummyBasePath.Length);
            }

            return normalizedUri.AbsolutePath;
        }
        else
        {
            Debug.LogError($"Could not normalize path '{path}' using URI class");
            return path;
        }
    }

    public static bool AreEqual(string pathA, string pathB)
    {
        return Path.GetFullPath(pathA) == Path.GetFullPath(pathB);
    }
}
