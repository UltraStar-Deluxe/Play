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
    [Inject(UxmlName = R.UxmlNames.networkConfigContainer)]
    private VisualElement networkConfigContainer;

    [Inject(UxmlName = R.UxmlNames.udpPortOnClientTextField)]
    private TextField udpPortOnClientTextField;

    [Inject(UxmlName = R.UxmlNames.udpPortOnServerTextField)]
    private TextField udpPortOnServerTextField;

    [Inject(UxmlName = R.UxmlNames.ownHostTextField)]
    private TextField ownHostTextField;

    [Inject]
    private Settings settings;

    [Inject]
    private GameObject gameObject;

    public void OnInjectionFinished()
    {
        // Only show controls when dev mode is enabled.
        UpdateNetworkConfigVisibility();
        settings.ObserveEveryValueChanged(it => it.IsDevModeEnabled)
            .Subscribe(_ => UpdateNetworkConfigVisibility())
            .AddTo(gameObject);

        // Update value when TextField changes
        BindTextField(udpPortOnServerTextField,
            () => settings.UdpPortOnServer,
            newStringValue => PropertyUtils.TrySetIntFromString(newStringValue, newIntValue => settings.UdpPortOnServer = newIntValue));

        BindTextField(udpPortOnClientTextField,
            () => settings.UdpPortOnClient,
            newStringValue => PropertyUtils.TrySetIntFromString(newStringValue, newIntValue => settings.UdpPortOnClient = newIntValue));

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

    private void UpdateNetworkConfigVisibility()
    {
        networkConfigContainer.SetVisibleByDisplay(settings.IsDevModeEnabled);
    }
}
