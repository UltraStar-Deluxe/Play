using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Text))]
public class ShowWhiteSpaceText : MonoBehaviour
{
    // Unicode BULLET (U+2022).
    // This (or a similar ) character is also used to indicate white-space in office word processing and notepad++.
    public static readonly string spaceReplacement = "•";
    // Unicode DOWNWARDS ARROW WITH CORNER LEFTWARDS (U+21B5)
    public static readonly string newlineReplacement = "↵\n";

    private Text uiText;

    // Breaking here with the code style to use the same "text" property name as Unity's standard Text Component.
    public string text
    {
        get
        {
            return ReplaceVisibleCharactersWithWhiteSpace(uiText.text);
        }
        set
        {
            uiText.text = ReplaceWhiteSpaceWithVisibleCharacters(value);
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
        uiText.text = ReplaceWhiteSpaceWithVisibleCharacters(uiText.text);
    }

    public static string ReplaceVisibleCharactersWithWhiteSpace(string text)
    {
        return text.Replace(spaceReplacement, " ")
            .Replace("\n", "")
            .Replace("↵", "\n");
    }

    public static string ReplaceWhiteSpaceWithVisibleCharacters(string text)
    {
        return text.Replace(" ", spaceReplacement)
            .Replace("\n", newlineReplacement);
    }
}
