using UnityEngine;

public class ShowWhiteSpaceUtils : MonoBehaviour
{
    // Unicode BULLET (U+2022).
    // This (or a similar ) character is also used to indicate white-space in office word processing and notepad++.
    public static readonly string spaceReplacement = "•";
    // Unicode DOWNWARDS ARROW WITH CORNER LEFTWARDS (U+21B5)
    public static readonly string newlineVisibleWhiteSpaceCharacter = "↵";
    public static readonly string newlineReplacement = newlineVisibleWhiteSpaceCharacter + "\n";

    /// <summary>
    /// Replaces visible whitespace replacements with real whitespace ("•" -> " " and
    /// "↵\n" -> "\n"). Also removes any lone newline replacement characters ("↵"). It is safe to
    /// call this function for text that already contains real whitespace, the real whitespace characters
    /// will not be modified in any way.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string ReplaceVisibleCharactersWithWhiteSpace(string text)
    {
        return text.Replace(spaceReplacement, " ")
            .Replace(newlineVisibleWhiteSpaceCharacter, "");
    }

    /// <summary>
    /// Replaces real whitespace with visible whitespace replacements (" " -> "•" and
    /// "\n" -> "↵\n"). Leaves existing "↵\n" replacements untouched to avoid stacking multiple
    /// replacements like ("↵\n" -> "↵↵\n"). It is safe to call this function for text that already
    /// contains visible whitespace replacements, the replacements will not be modified in any way.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static string ReplaceWhiteSpaceWithVisibleCharacters(string text)
    {
        return text.Replace(" ", spaceReplacement)
            .Replace(newlineReplacement, "⌇")
            .Replace("\n", newlineReplacement)
            .Replace("⌇", newlineReplacement);
    }
}
