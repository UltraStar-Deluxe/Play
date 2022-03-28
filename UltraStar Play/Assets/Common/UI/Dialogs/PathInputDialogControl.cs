using System;
using System.IO;
using UniRx;

public class PathInputDialogControl : TextInputDialogControl
{
    public override void OnInjectionFinished()
    {
        if (backslashReplacingTextFieldControl == null)
        {
            backslashReplacingTextFieldControl = new PathTextFieldControl(textField);
            backslashReplacingTextFieldControl.ValueChangedEventStream
                .Subscribe(newValue => ValidateValue(newValue, true));
        }

        base.OnInjectionFinished();

        ValidateValueCallback = newValue =>
        {
            if (newValue.IsNullOrEmpty())
            {
                return ValueInputDialogValidationResult.CreateErrorResult("Enter a value please");
            }

            if (File.Exists(newValue)
                || Directory.Exists(newValue))
            {
                return ValueInputDialogValidationResult.CreateValidResult();
            }

            return ValueInputDialogValidationResult.CreateErrorResult("File not found");
        };
    }
}
