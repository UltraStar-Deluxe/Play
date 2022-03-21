using System;
using UnityEngine;

public class ShowWhiteSpaceText : MonoBehaviour
{
    // Unicode BULLET (U+2022).
    // This (or a similar ) character is also used to indicate white-space in office word processing and notepad++.
    public static readonly string spaceReplacement = "•";
    // Unicode DOWNWARDS ARROW WITH CORNER LEFTWARDS (U+21B5)
    public static readonly string newlineReplacement = "↵\n";

    public static string ReplaceVisibleCharactersWithWhiteSpace(string text)
    {
        return text.Replace(spaceReplacement, " ")
            .Replace("↵\n", "\n")
            .Replace("↵", "\n");
    }

    public static string ReplaceWhiteSpaceWithVisibleCharacters(string text)
    {
        return text.Replace(" ", spaceReplacement)
            .Replace("\n", newlineReplacement);
    }
}
