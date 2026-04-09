using UnityEngine;

public class ShowWhiteSpaceUtils : MonoBehaviour
{
    // Unicode BULLET (U+2022).
    // This (or a similar ) character is also used to indicate white-space in office word processing and notepad++.
    public static readonly string spaceReplacement = "•";
    // Unicode DOWNWARDS ARROW WITH CORNER LEFTWARDS (U+21B5)
    public static readonly string newlineVisibleWhiteSpaceCharacter = "↵";
    public static readonly string newlineReplacement = newlineVisibleWhiteSpaceCharacter + "\n";

    public static string ReplaceVisibleCharactersWithWhiteSpace(string text)
    {
        return text.Replace(spaceReplacement, " ")
            // Remove whitespace character or newline character where the counterpart is missing
            .Replace(newlineReplacement, "⌇")
            .Replace("\n", "")
            .Replace(newlineVisibleWhiteSpaceCharacter, "")
            .Replace("⌇", "\n");
    }

    public static string ReplaceWhiteSpaceWithVisibleCharacters(string text)
    {
        return text.Replace(" ", spaceReplacement)
            .Replace("\n", newlineReplacement);
    }
}
