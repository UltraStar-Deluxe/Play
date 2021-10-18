using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.EventSystems;
using System.Text.RegularExpressions;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorNoteLyricsInputField : MonoBehaviour, INeedInjection
{
    public EditorUiNote EditorUiNote => uiEditorNote;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private InputField inputField;

    private EditorUiNote uiEditorNote;

    private static readonly Regex whitespaceRegex = new Regex(@"^\s+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Text
    {
        get
        {
            return inputField.text;
        }
        set
        {
            inputField.text = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(value);
        }
    }

    void Start()
    {
        RequestFocus();

        inputField.onValidateInput += OnValidateInput;
        inputField.onEndEdit.AsObservable().Subscribe(OnEndEdit);
    }

    private char OnValidateInput(string text, int charIndex, char addedChar)
    {
        if (addedChar == ' ')
        {
            return ShowWhiteSpaceText.spaceReplacement[0];
        }
        return addedChar;
    }

    public void Init(EditorUiNote uiEditorNote, string text)
    {
        this.uiEditorNote = uiEditorNote;
        Text = text;
    }

    public void RequestFocus()
    {
        EventSystem.current.SetSelectedGameObject(gameObject, null);
        inputField.ActivateInputField();
    }

    private void OnValueChanged(string newInputFieldText)
    {
        string newSimpleText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(newInputFieldText);
        if (ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(inputField.text) != newSimpleText)
        {
            inputField.text = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(newSimpleText);
        }
    }

    private void OnEndEdit(string inputFieldText)
    {
        string newText = ShowWhiteSpaceText.ReplaceVisibleCharactersWithWhiteSpace(inputFieldText);

        // Replace multiple control characters with a single character
        newText = Regex.Replace(newText, @"\s+", " ");
        newText = Regex.Replace(newText, @";+", ";");

        // Replace any text after control characters.
        // Otherwise the text would mess up following notes when using the LyricsArea.
        newText = Regex.Replace(newText, @" .+", " ");
        newText = Regex.Replace(newText, @";.+", ";");

        if (!IsOnlyWhitespace(inputFieldText))
        {
            uiEditorNote.Note.SetText(newText);
            uiEditorNote.SetLyrics(newText);

            songMetaChangeEventStream.OnNext(new LyricsChangedEvent());
        }

        inputField.onValidateInput -= OnValidateInput;

        Destroy(gameObject);
    }

    private bool IsOnlyWhitespace(string newText)
    {
        return string.IsNullOrEmpty(newText) || whitespaceRegex.IsMatch(newText);
    }
}
