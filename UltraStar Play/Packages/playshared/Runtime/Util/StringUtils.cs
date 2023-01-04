public static class StringUtils
{
    // Returns a nicely readable string from a filename, e.g. "theme_blue" will become "Theme Blue"
    public static string SnakeCaseToDisplayName(string input)
    {
        input = input.Replace("_", " ").Replace("-", " ");
        char[] chars = input.ToCharArray();
        bool lastWasSpace = true;
        for (int c = 0; c < chars.Length; c++)
        {
            if (lastWasSpace) chars[c] = char.ToUpperInvariant(input[c]);
            lastWasSpace = char.IsWhiteSpace(chars[c]);
        }
        return new string(chars);
    }
}
