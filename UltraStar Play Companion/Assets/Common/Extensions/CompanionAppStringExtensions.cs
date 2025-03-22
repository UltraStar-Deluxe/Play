using System;

public static class CompanionAppStringExtensions
{
    public static string ReplaceOrThrow(this string text, string pattern, string replacement)
    {
        if (!text.Contains(pattern))
        {
            throw new ArgumentException($"Text '{text}' does not contain pattern '{pattern}'");
        }
        return text.Replace(pattern, replacement);
    }
}
