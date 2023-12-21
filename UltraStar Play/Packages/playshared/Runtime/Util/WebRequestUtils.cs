public static class WebRequestUtils
{
    public static bool IsHttpOrHttpsUri(string uri)
    {
        return !uri.IsNullOrEmpty()
                && (uri.StartsWith("http://")
                    || uri.StartsWith("https://"));
    }

    public static bool IsNetworkPath(string absolutePath)
    {
        return !absolutePath.IsNullOrEmpty()
               && (absolutePath.StartsWith(@"\\")
                   || absolutePath.StartsWith("//"));
    }

    public static string AbsoluteFilePathToUri(string absolutePath)
    {
        if (absolutePath.StartsWith(@"\\"))
        {
            // This is a Windows-like network path.
            // MUST prefix it with the file:// scheme AND an additional slash for Unity API to work.
            // See https://forum.unity.com/threads/unitywebrequest-and-local-area-network.714353/
            return "file:///" + absolutePath;
        }

        if (absolutePath.StartsWith("//"))
        {
            // This also is a Unix-like network path. But because forward slashes are used, MUST prefix it with the file:// scheme ONLY for Unity API to work.
            return "file://" + absolutePath;
        }

        // This is a local path. MUST NOT prefix it with the file:// scheme.
        // Otherwise some paths may not work, e.g., when it contains a space AND a plus character.
        // See https://forum.unity.com/threads/unitywebrequest-file-protocol-not-working-with-plus-character-in-path-how-to-escape-the-uri.1364499/#post-8655012
        return absolutePath;
    }
}
