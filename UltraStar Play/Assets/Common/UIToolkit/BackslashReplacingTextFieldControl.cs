using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

/**
 * Workaround for automatically escaped characters (e.g. \n) in TextField.
 * See https://forum.unity.com/threads/preventing-escaped-characters-in-textfield.1071425/
 */
public class BackslashReplacingTextFieldControl : MonoBehaviour, INeedInjection
{
    private const string BackslashReplacement = "＼";

    private TextField textField;

    private readonly Subject<string> valueChangedEventStream = new Subject<string>();
    public IObservable<string> ValueChangedEventStream => valueChangedEventStream;

    public BackslashReplacingTextFieldControl(TextField textField)
    {
        textField.RegisterValueChangedCallback(evt =>
        {
            string newValueEscaped = EscapeBackslashes(evt.newValue);
            string newValueUnescaped = UnescapeBackslashes(newValueEscaped);
            textField.SetValueWithoutNotify(newValueEscaped);

            valueChangedEventStream.OnNext(newValueUnescaped);
        });

        textField.value = textField.value.Replace("\\", BackslashReplacement);
    }

    public static string EscapeBackslashes(string text)
    {
        return text.Replace("\\", BackslashReplacement);
    }

    public static string UnescapeBackslashes(string text)
    {
        return text.Replace(BackslashReplacement, "\\");
    }
}
