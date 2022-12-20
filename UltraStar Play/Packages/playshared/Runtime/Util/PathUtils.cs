public static class PathUtils
{
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
        return c == System.IO.Path.DirectorySeparatorChar
               || c == System.IO.Path.AltDirectorySeparatorChar;
    }

    private static bool IsValidDriveChar(char c)
    {
        return c >= 'A' && c <= 'Z'
               || c >= 'a' && c <= 'z';
    }
}
