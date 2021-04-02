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
    
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private Text uiText;

    private readonly List<IDisposable> disposables = new List<IDisposable>();
    
    private void Start()
    {
        UpdateConnectedClientCountText();
        disposables.Add(serverSideConnectRequestManager.ClientConnectedEventStream
            .Subscribe(_ => UpdateConnectedClientCountText()));
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

    private void OnDestroy()
    {
        disposables.ForEach(it => it.Dispose());
    }
}
