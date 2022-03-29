using System;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Workaround for automatically escaped characters (e.g. \n) in TextField.
 * See https://forum.unity.com/threads/preventing-escaped-characters-in-textfield.1071425/
 */
public class BackslashReplacingTextFieldControl
{
    private const string DefaultBackslashReplacement = "＼";

    private TextField textField;

    private readonly Subject<string> valueChangedEventStream = new Subject<string>();
    public IObservable<string> ValueChangedEventStream => valueChangedEventStream;

    private readonly string backslashReplacement;

    public BackslashReplacingTextFieldControl(TextField textField, string backslashReplacement = DefaultBackslashReplacement)
    {
        this.backslashReplacement = backslashReplacement;
        textField.RegisterValueChangedCallback(evt =>
        {
            string newValueEscaped = EscapeBackslashes(evt.newValue);
            string newValueUnescaped = UnescapeBackslashes(newValueEscaped);
            textField.SetValueWithoutNotify(newValueEscaped);

            valueChangedEventStream.OnNext(newValueUnescaped);
        });

        textField.value = textField.value.Replace("\\", backslashReplacement);
    }

    public string EscapeBackslashes(string text)
    {
        return text.Replace("\\", backslashReplacement);
    }

    public string UnescapeBackslashes(string text)
    {
        return text.Replace(backslashReplacement, "\\");
    }
}
