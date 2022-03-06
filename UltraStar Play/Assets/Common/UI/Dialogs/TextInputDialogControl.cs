using System;
using System.Linq;
using UniRx;
using UnityEngine.UIElements;

public class TextInputDialogControl : IDialogControl
{
    private readonly VisualElement dialogRootVisualElement;
    private readonly VisualElement parentVisualElement;

    private readonly VisualElement invalidValueIcon;
    private readonly Label invalidValueLabel;
    private readonly Button okButton;

    private readonly Subject<bool> dialogClosedEventStream = new Subject<bool>();
    public IObservable<bool> DialogClosedEventStream => dialogClosedEventStream;

    private readonly Subject<string> submitValueEventStream = new Subject<string>();
    public IObservable<string> SubmitValueEventStream => submitValueEventStream;

    public Func<string, ValueInputDialogValidationResult> ValidateValueCallback { get; set; } = DefaultValidateValueCallback;

    public TextInputDialogControl(
        VisualTreeAsset dialogUi,
        VisualElement parentVisualElement,
        string title,
        string message,
        string initialTextValue)
    {
        dialogRootVisualElement = dialogUi.CloneTree();
        dialogRootVisualElement.AddToClassList("overlay");

        Label dialogTitle = dialogRootVisualElement.Q<Label>(R.UxmlNames.dialogTitle);
        dialogTitle.text = title;

        Label dialogMessage = dialogRootVisualElement.Q<Label>(R.UxmlNames.dialogMessage);
        dialogMessage.text = message;

        invalidValueIcon = dialogRootVisualElement.Q<VisualElement>(R.UxmlNames.invalidValueIcon);
        invalidValueLabel = dialogRootVisualElement.Q<Label>(R.UxmlNames.invalidValueLabel);

        TextField textField = dialogRootVisualElement.Q<TextField>(R.UxmlNames.valueTextField);
        textField.value = initialTextValue;
        textField.Focus();

        BackslashReplacingTextFieldControl backslashReplacingTextFieldControl = new BackslashReplacingTextFieldControl(textField);
        backslashReplacingTextFieldControl.ValueChangedEventStream
            .Subscribe(newValue => ValidateValue(newValue, true));

        okButton = dialogRootVisualElement.Q<Button>(R.UxmlNames.okButton);
        okButton.RegisterCallbackButtonTriggered(() => TrySubmitValue(textField.value));
        Button cancelButton = dialogRootVisualElement.Q<Button>(R.UxmlNames.cancelButton);
        cancelButton.RegisterCallbackButtonTriggered(() => CloseDialog());

        this.parentVisualElement = parentVisualElement;
        parentVisualElement.Add(dialogRootVisualElement);

        ValidateValue(initialTextValue, false);
    }

    private void TrySubmitValue(string textValue)
    {
        if (ValidateValueCallback == null
            || ValidateValueCallback(textValue).Severity != EValueInputDialogValidationResultSeverity.Error)
        {
            submitValueEventStream.OnNext(textValue);
            CloseDialog();
        }
    }

    private void ValidateValue(string textValue, bool showMessageIfInvalid)
    {
        if (ValidateValueCallback == null)
        {
            HideValidationMessage();
            return;
        }

        ValueInputDialogValidationResult validationResult = ValidateValueCallback(textValue);
        if (validationResult.Severity == EValueInputDialogValidationResultSeverity.None)
        {
            HideValidationMessage();
        }
        else
        {
            okButton.SetEnabled(false);
            if (showMessageIfInvalid)
            {
                invalidValueIcon.ShowByVisibility();
                invalidValueLabel.ShowByVisibility();
            }

            invalidValueLabel.text = validationResult.Message;
            if (validationResult.Severity == EValueInputDialogValidationResultSeverity.Warning)
            {
                invalidValueIcon.AddToClassList("warning");
            }
            else if (validationResult.Severity == EValueInputDialogValidationResultSeverity.Error)
            {
                invalidValueIcon.AddToClassList("error");
            }
        }
    }

    private void HideValidationMessage()
    {
        invalidValueIcon.HideByVisibility();
        invalidValueLabel.HideByVisibility();
        invalidValueIcon.RemoveFromClassList("warning");
        invalidValueIcon.RemoveFromClassList("error");
        okButton.SetEnabled(true);
    }

    private static ValueInputDialogValidationResult DefaultValidateValueCallback(string newValue)
    {
        if (newValue.IsNullOrEmpty())
        {
            return ValueInputDialogValidationResult.CreateErrorResult("Enter a value please");
        }
        return ValueInputDialogValidationResult.CreateValidResult();
    }

    public void CloseDialog()
    {
        parentVisualElement.Remove(dialogRootVisualElement);
        dialogClosedEventStream.OnNext(true);
    }
}
