using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class PathTextFieldControl : BackslashReplacingTextFieldControl
{
    private const string BackslashReplacement = "＼"; // FULLWIDTH REVERSE SOLIDUS

    public PathTextFieldControl(TextField textField)
        : base(textField, BackslashReplacement)
    {
    }
}
