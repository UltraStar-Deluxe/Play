using System;
using System.IO;

public class PathInputDialogControl : TextInputDialogControl
{
    public override void OnInjectionFinished()
    {
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
