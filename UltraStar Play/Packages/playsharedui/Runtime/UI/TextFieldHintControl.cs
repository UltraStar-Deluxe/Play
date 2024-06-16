using System;
using UnityEngine.UIElements;

public class TextFieldHintControl
{
    private readonly Label hintLabel;
    private readonly TextField textField;

    public Translation Hint
    {
        get => Translation.Of(hintLabel.text);
        set => hintLabel.SetTranslatedText(value);
    }

    public TextFieldHintControl(TextField textField)
    {
        this.textField = textField;
        hintLabel = textField.Q<Label>(null, "textFieldHint");
        if (hintLabel == null)
        {
            throw new Exception("Hint label of TextField not found in children");
        }

        Init();
    }

    public TextFieldHintControl(Label hintLabel)
    {
        this.hintLabel = hintLabel;
        textField = this.hintLabel.GetFirstAncestorOfType<TextField>();
        if (textField == null)
        {
            throw new Exception("TextField of hint label not found in parent elements");
        }

        Init();
    }

    private void Init()
    {
        textField.RegisterValueChangedCallback(evt =>
        {
            UpdateHintLabel();
        });
        textField.RegisterCallback<FocusEvent>(evt => UpdateHintLabel());
        textField.RegisterCallback<BlurEvent>(evt => UpdateHintLabel(true));
        UpdateHintLabel();
    }

    private void UpdateHintLabel(bool isBlurEvent = false)
    {
        hintLabel.SetVisibleByDisplay(textField.value.IsNullOrEmpty()
            && (isBlurEvent ||
                (textField.focusController == null
                 || textField.focusController.focusedElement != textField)));
    }
}
