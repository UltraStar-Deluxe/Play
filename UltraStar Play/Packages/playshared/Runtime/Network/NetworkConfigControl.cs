using System;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NetworkConfigControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = "ipPortOnServerTextField")]
    protected TextField ipPortOnServerTextField;

    [Inject(UxmlName = "ownHostTextField")]
    protected TextField ownHostTextField;

    [Inject]
    protected ISettings settings;

    public virtual void OnInjectionFinished()
    {
        // Update value when TextField changes
        BindTextField(ipPortOnServerTextField,
            () => settings.IpPortOnServer,
            newStringValue => PropertyUtils.TrySetIntFromString(newStringValue, newIntValue => settings.IpPortOnServer = newIntValue));

        BindTextField(ownHostTextField,
            () => settings.OwnHost,
            newStringValue => settings.OwnHost = newStringValue);
    }

    private void BindTextField(TextField textField, Func<object> valueGetter, Action<string> valueSetter)
    {
        object initialValue = valueGetter();
        textField.value = initialValue != null
            ? initialValue.ToString()
            : "";

        textField.RegisterValueChangedCallback(evt => valueSetter(evt.newValue));
    }
}
