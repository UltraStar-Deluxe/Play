using System;
using System.IO;
using UnityEngine.UIElements;

public class PathInputDialogControl : TextInputDialogControl
{
    public PathInputDialogControl(
        VisualTreeAsset dialogUi,
        VisualElement parentVisualElement,
        string title,
        string message,
        string initialTextValue)
        : base(dialogUi, parentVisualElement, title, message, initialTextValue)
    {
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
