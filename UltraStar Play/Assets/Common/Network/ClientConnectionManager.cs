using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

public static class ClientConnectionManager
{
    private static Dictionary<string, ConnectedClientHandler> idToConnectedClientMap = new Dictionary<string, ConnectedClientHandler>();

    private static Subject<ClientConnectedEvent> clientConnectedEventStream = new Subject<ClientConnectedEvent>();
    public static IObservable<ClientConnectedEvent> ClientConnectedEventStream => clientConnectedEventStream;
    
    public static void RemoveAllLocalClients()
    {
        idToConnectedClientMap.Values.ForEach(it => it.Dispose());
        idToConnectedClientMap.Clear();
    }

    public static ConnectedClientHandler RegisterClient(
        IPEndPoint clientIpEndPoint,
        string clientName,
        int microphoneSampleRate)
    {
        // Dispose any currently registered client with the same IP-Address.
        if (idToConnectedClientMap.TryGetValue(GetClientId(clientIpEndPoint), out ConnectedClientHandler existingConnectedClientHandler))
        {
            existingConnectedClientHandler.Dispose();
        }
        
        ConnectedClientHandler connectedClientHandler = new ConnectedClientHandler(clientIpEndPoint, clientName, microphoneSampleRate);
        idToConnectedClientMap[GetClientId(clientIpEndPoint)] = connectedClientHandler;
        
        clientConnectedEventStream.OnNext(new ClientConnectedEvent(connectedClientHandler));
        
        return connectedClientHandler;
    }

    public static List<ConnectedClientHandler> GetConnectedClientHandlers()
    {
        return idToConnectedClientMap.Values.ToList();
    }
    
    public static bool TryGetConnectedClientHandler(string clientIpEndPointId, out ConnectedClientHandler connectedClientHandler)
    {
        return idToConnectedClientMap.TryGetValue(clientIpEndPointId, out connectedClientHandler);
    }

    public static string GetClientId(IPEndPoint clientIpEndPoint)
    {
        return clientIpEndPoint.Address.ToString();
    }
}
