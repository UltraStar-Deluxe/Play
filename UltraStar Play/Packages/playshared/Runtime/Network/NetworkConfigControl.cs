using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NetworkConfigControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = "networkConfigContainer")]
    protected VisualElement networkConfigContainer;

    [Inject(UxmlName = "udpPortOnClientTextField")]
    protected TextField udpPortOnClientTextField;

    [Inject(UxmlName = "udpPortOnServerTextField")]
    protected TextField udpPortOnServerTextField;

    [Inject(UxmlName = "ownHostTextField")]
    protected TextField ownHostTextField;

    [Inject]
    protected ISettings settings;

    [Inject]
    protected GameObject gameObject;

    public virtual void OnInjectionFinished()
    {
        // Update value when TextField changes
        BindTextField(udpPortOnServerTextField,
            () => settings.UdpPortOnServer.Value,
            newStringValue => PropertyUtils.TrySetIntFromString(newStringValue, newIntValue => settings.UdpPortOnServer.Value = newIntValue));

        BindTextField(udpPortOnClientTextField,
            () => settings.UdpPortOnClient.Value,
            newStringValue => PropertyUtils.TrySetIntFromString(newStringValue, newIntValue => settings.UdpPortOnClient.Value = newIntValue));

        BindTextField(ownHostTextField,
            () => settings.OwnHost.Value,
            newStringValue => settings.OwnHost.Value = newStringValue);
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
