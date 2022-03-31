using System.Text.RegularExpressions;
using UniInject;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class EditorNoteLyricsInputControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongEditorSceneControl songEditorSceneControl;

    [Inject]
    private EditorNoteControl editorNoteControl;

    [Inject(UxmlName = R.UxmlNames.editLyricsPopup)]
    private VisualElement editLyricsPopup;

    [Inject(UxmlName = R.UxmlNames.editLyricsPopupTextField)]
    private TextField textField;

    private bool isActive;

    private static readonly Regex whitespaceRegex = new(@"^\s+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public void OnInjectionFinished()
    {
        textField.value = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(editorNoteControl.Note.Text);
        textField.Focus();
        RegisterEvents();
    }

    private void OnTextFieldValueChanged(string newInputFieldText)
    {
        string visibleWhitespaceText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(newInputFieldText);
        if (textField.value != visibleWhitespaceText)
        {
            textField.value = visibleWhitespaceText;
        }
    }

    public void SubmitAndCloseLyricsDialog()
    {
        string newText = ShowWhiteSpaceText.ReplaceVisibleCharactersWithWhiteSpace(textField.value);

        // Replace multiple control characters with a single character
        newText = Regex.Replace(newText, @"\s+", " ");
        newText = Regex.Replace(newText, @";+", ";");

        // Replace any text after control characters.
        // Otherwise the text would mess up following notes when using the LyricsArea.
        newText = Regex.Replace(newText, @" .+", " ");
        newText = Regex.Replace(newText, @";.+", ";");

        if (!IsOnlyWhitespace(newText))
        {
            editorNoteControl.Note.SetText(newText);
            editorNoteControl.SetLyrics(newText);
            songMetaChangeEventStream.OnNext(new LyricsChangedEvent());
        }

        UnregisterEvents();
        songEditorSceneControl.HideEditLyricsPopup();
        if (textField.focusController.focusedElement == textField)
        {
            textField.Blur();
        }
    }

    private bool IsOnlyWhitespace(string newText)
    {
        return string.IsNullOrEmpty(newText) || whitespaceRegex.IsMatch(newText);
    }

    private void RegisterEvents()
    {
        textField.RegisterValueChangedCallback(ValueChangedCallback);
        textField.RegisterCallback<BlurEvent>(OnBlur);
        isActive = true;
    }

    private void UnregisterEvents()
    {
        textField.UnregisterValueChangedCallback(ValueChangedCallback);
        textField.UnregisterCallback<BlurEvent>(OnBlur);
        isActive = false;
    }

    private void ValueChangedCallback(ChangeEvent<string> evt)
    {
        OnTextFieldValueChanged(evt.newValue);
    }

    private void OnBlur(BlurEvent evt)
    {
        SubmitAndCloseLyricsDialog();
        UnregisterEvents();
    }

    public bool IsActive()
    {
        return isActive;
    }
}
