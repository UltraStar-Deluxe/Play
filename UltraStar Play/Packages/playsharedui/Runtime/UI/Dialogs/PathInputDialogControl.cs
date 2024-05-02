using System.IO;
using UnityEngine.UIElements;

public class PathInputDialogControl : TextInputDialogControl
{
    public override void OnInjectionFinished()
    {
        textField.RegisterValueChangedCallback(evt => ValidateValue(evt.newValue, true));

        base.OnInjectionFinished();

        ValidateValueCallback = newValue =>
        {
            if (newValue.IsNullOrEmpty())
            {
                return ValueInputDialogValidationResult.CreateErrorResult(Translation.Get("validation_missingValue"));
            }

            if (File.Exists(newValue)
                || Directory.Exists(newValue))
            {
                return ValueInputDialogValidationResult.CreateValidResult();
            }

            return ValueInputDialogValidationResult.CreateErrorResult(Translation.Get("common_error_fileNotFound"));
        };
    }
}
