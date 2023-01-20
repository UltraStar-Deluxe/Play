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
}
