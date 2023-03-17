using System.IO;
using UniRx;
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
