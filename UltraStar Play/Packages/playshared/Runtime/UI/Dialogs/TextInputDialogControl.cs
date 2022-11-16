using System;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

public class TextInputDialogControl : AbstractDialogControl, IInjectionFinishedListener
{
    [Inject(UxmlName = "dialogTitle")]
    protected Label dialogTitle;

    [Inject(UxmlName = "dialogMessage")]
    protected Label dialogMessage;

    [Inject(UxmlName = "invalidValueIcon")]
    protected VisualElement invalidValueIcon;

    [Inject(UxmlName = "invalidValueLabel")]
    protected Label invalidValueLabel;

    [Inject(UxmlName = "valueTextField")]
    protected TextField textField;

    [Inject(UxmlName = "okButton")]
    protected Button okButton;

    [Inject(UxmlName = "cancelButton")]
    protected Button cancelButton;

    private readonly Subject<string> submitValueEventStream = new();
    public IObservable<string> SubmitValueEventStream => submitValueEventStream;

    public Func<string, ValueInputDialogValidationResult> ValidateValueCallback { get; set; } = DefaultValidateValueCallback;

    public string Title
    {
        get
        {
            return dialogTitle.text;
        }

        set
        {
            dialogTitle.text = value;
        }
    }

    public string Message
    {
        get
        {
            return dialogMessage.text;
        }

        set
        {
            dialogMessage.text = value;
        }
    }

    public string Value
    {
        get
        {
            return textField.value;
        }

        set
        {
            textField.value = value;
        }
    }

    private string initialValue = "";
    public string InitialValue
    {
        get
        {
            return initialValue;
        }

        set
        {
            initialValue = value;
            Value = value;

            // Do not show an error if the user has not done anything yet.
            // But do not allow the user to continue if invalid either (okButton would still be disabled).
            HideValidationMessage();
        }
    }

    public virtual void OnInjectionFinished()
    {
        okButton.RegisterCallbackButtonTriggered(() => TrySubmitValue(textField.value));
        cancelButton.RegisterCallbackButtonTriggered(() => CloseDialog());
        textField.RegisterValueChangedCallback(evt => ValidateValue(evt.newValue, true));

        cancelButton.Focus();
        InitialValue = "";
        ValidateValue(InitialValue, false);
    }

    public void Reset()
    {
        Value = initialValue;
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

    protected void ValidateValue(string textValue, bool showMessageIfInvalid)
    {
        if (ValidateValueCallback == null)
        {
            HideValidationMessageAndEnableOkButton();
            return;
        }

        ValueInputDialogValidationResult validationResult = ValidateValueCallback(textValue);
        if (validationResult.Severity == EValueInputDialogValidationResultSeverity.None)
        {
            HideValidationMessageAndEnableOkButton();
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

    private void HideValidationMessageAndEnableOkButton()
    {
        HideValidationMessage();
        okButton.SetEnabled(true);
    }

    private void HideValidationMessage()
    {
        invalidValueIcon.HideByVisibility();
        invalidValueLabel.HideByVisibility();
        invalidValueIcon.RemoveFromClassList("warning");
        invalidValueIcon.RemoveFromClassList("error");
    }

    private static ValueInputDialogValidationResult DefaultValidateValueCallback(string newValue)
    {
        if (newValue.IsNullOrEmpty())
        {
            return ValueInputDialogValidationResult.CreateErrorResult("Enter a value please");
        }
        return ValueInputDialogValidationResult.CreateValidResult();
    }
}
