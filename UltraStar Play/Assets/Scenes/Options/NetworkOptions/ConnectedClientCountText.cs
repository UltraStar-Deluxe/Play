using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ConnectedClientCountText : MonoBehaviour, INeedInjection
{
    [Inject]
    private ServerSideConnectRequestManager serverSideConnectRequestManager; 
    
    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;

    private void Start()
    {
        UpdateConnectedClientCountText();
        serverSideConnectRequestManager.ClientConnectedEventStream
            .Subscribe(_ => UpdateConnectedClientCountText())
            .AddTo(gameObject);
    }

    private void UpdateConnectedClientCountText()
    {
        string connectedClientNamesCsv = ServerSideConnectRequestManager.ConnectedClientCount > 0
            ? ServerSideConnectRequestManager.GetConnectedClientHandlers()
                .Select(it => it.ClientName)
                .ToCsv(", ", "", "")
            : "";
        uiText.text = $"Connected Clients: {ServerSideConnectRequestManager.ConnectedClientCount}\n"
            + connectedClientNamesCsv;
    }
}
