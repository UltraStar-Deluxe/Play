using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class ShowWhiteSpaceText : MonoBehaviour
{
    // Unicode Middle Dot (U+00B7).
    // This (or a similar ) character is also used to indicate white-space in office word processing and notepad++.
    public static readonly char spaceReplacementCharacter = '·';
    public static readonly string spaceReplacementHexColor = "FF00FF";
    public static readonly string spaceReplacementRichText = $"<b><color=#{spaceReplacementHexColor}>{spaceReplacementCharacter}</color></b>";
    public static readonly string nospaceReplacementRichText = $"<b><color=#{spaceReplacementHexColor}></color></b>";

    private Text uiText;

    // Breaking here with the code style to use the same "text" property name as Unity's standard Text Component.
    public string text
    {
        get
        {
            return ReplaceVisibleCharactersRichTextWithWhiteSpace(uiText.text);
        }
        set
        {
            uiText.text = ReplaceWhiteSpaceWithVisibleCharactersRichText(value);
        }
    }

    private void Awake()
    {
        uiText = GetComponent<Text>();
    }

    void Start()
    {
        UpdateUiText();
    }

    void UpdateUiText()
    {
        uiText.text = ReplaceWhiteSpaceWithVisibleCharactersRichText(uiText.text);
    }

    public static string ReplaceVisibleCharactersRichTextWithWhiteSpace(string text)
    {
        return text
            .Replace(spaceReplacementRichText, " ")
            .Replace(nospaceReplacementRichText, "");
    }

    public static string ReplaceWhiteSpaceWithVisibleCharactersRichText(string text)
    {
        return text.Replace(" ", spaceReplacementRichText);
    }

    public static string ReplaceVisibleCharactersWithWhiteSpace(string text)
    {
        return text.Replace(spaceReplacementCharacter, ' ');
    }

    public static string ReplaceWhiteSpaceWithVisibleCharacters(string text)
    {
        return text.Replace(' ', spaceReplacementCharacter);
    }
}
