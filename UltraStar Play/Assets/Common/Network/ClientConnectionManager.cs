using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

public static class ClientConnectionManager
{
    private static Dictionary<string, ConnectedClientHandler> idToConnectedClientMap = new Dictionary<string, ConnectedClientHandler>();

    public static void RemoveAllLocalClients()
    {
        idToConnectedClientMap.Values.ForEach(it => it.Dispose());
        idToConnectedClientMap.Clear();
    }
    
    public static bool HasConnectedClient(string clientId)
    {
        return idToConnectedClientMap.ContainsKey(clientId);
    }

    public static ConnectedClientHandler RegisterClient(string clientId, int microphoneSampleRate)
    {
        if (idToConnectedClientMap.ContainsKey(clientId))
        {
            throw new ConnectRequestException($"Client '{clientId}' already connected. Try using a different ClientId.");
        }

        ConnectedClientHandler connectedClientHandler = new ConnectedClientHandler(clientId, microphoneSampleRate);
        idToConnectedClientMap.Add(clientId, connectedClientHandler);
        return connectedClientHandler;
    }
}
