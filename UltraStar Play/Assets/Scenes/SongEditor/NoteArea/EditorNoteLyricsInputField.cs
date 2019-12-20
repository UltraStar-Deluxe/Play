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
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private InputField inputField;

    private UiEditorNote uiEditorNote;

    private static readonly Regex whitespaceRegex = new Regex(@"\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Text
    {
        get
        {
            return inputField.text;
        }
        set
        {
            inputField.text = value;
        }
    }

    void Start()
    {
        RequestFocus();
    }

    void Update()
    {
        if (!inputField.isFocused)
        {
            OnFocusLost();
        }
    }

    public void Init(UiEditorNote uiEditorNote, string text)
    {
        this.uiEditorNote = uiEditorNote;
        Text = text;
    }

    public void RequestFocus()
    {
        EventSystem.current.SetSelectedGameObject(gameObject, null);
        inputField.ActivateInputField();
    }

    private void OnFocusLost()
    {
        string newText = inputField.text;
        if (!IsOnlyWhitespace(newText))
        {
            uiEditorNote.SetText(newText);
        }
        uiEditorNote.EndEditingNoteText();
    }

    private bool IsOnlyWhitespace(string newText)
    {
        return string.IsNullOrEmpty(newText) || whitespaceRegex.IsMatch(newText);
    }
}
