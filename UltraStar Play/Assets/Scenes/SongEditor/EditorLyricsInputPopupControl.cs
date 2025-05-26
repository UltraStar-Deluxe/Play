using UniInject;
using UniRx;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class EditorLyricsInputPopupControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    protected SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject]
    protected SongEditorSceneControl songEditorSceneControl;

    [Inject(UxmlName = R.UxmlNames.editLyricsPopupTextField)]
    protected TextField textField;

    private string initialText;

    private bool isActive;

    protected abstract string GetInitialText();
    protected abstract void ApplyNewText(string newText);
    protected abstract void PreviewNewText(string newText);

    public virtual void OnInjectionFinished()
    {
        textField.DisableParseEscapeSequences();
        textField.selectAllOnFocus = false;
        textField.selectAllOnMouseUp = false;
        initialText = GetInitialText();
        textField.value = initialText;
        textField.Focus();
        RegisterEvents();
    }

    private void OnTextFieldValueChanged(string newInputFieldText)
    {
        string visibleWhitespaceText = ShowWhiteSpaceUtils.ReplaceWhiteSpaceWithVisibleCharacters(newInputFieldText);
        if (textField.value != visibleWhitespaceText)
        {
            textField.value = visibleWhitespaceText;
        }
        PreviewNewText(visibleWhitespaceText);
    }

    public void UndoAndCloseLyricsDialog()
    {
        ApplyNewText(initialText);
        CloseLyricsDialog();
    }

    public void SubmitAndCloseLyricsDialog()
    {
        string newText = ShowWhiteSpaceUtils.ReplaceVisibleCharactersWithWhiteSpace(textField.value);
        ApplyNewText(newText);
        CloseLyricsDialog();
    }

    private void CloseLyricsDialog()
    {
        UnregisterEvents();
        songEditorSceneControl.HideEditLyricsPopup();
        if (textField.focusController.focusedElement == textField)
        {
            // Move focus away
            textField.Blur();
            AwaitableUtils.ExecuteAfterDelayInFramesAsync(1, () => textField.GetRootVisualElement().Q<Button>().Focus());
        }
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
        if (Keyboard.current != null
            && Keyboard.current.escapeKey.isPressed)
        {
            UndoAndCloseLyricsDialog();
        }
        else
        {
            SubmitAndCloseLyricsDialog();
        }
    }

    public bool IsActive()
    {
        return isActive;
    }
}
