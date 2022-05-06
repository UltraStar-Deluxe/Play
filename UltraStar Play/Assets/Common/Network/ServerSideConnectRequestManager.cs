using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using SimpleHttpServerForUnity;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ServerSideConnectRequestManager : MonoBehaviour, INeedInjection, IServerSideConnectRequestManager
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void InitOnLoad()
    {
        instance = null;
        idToConnectedClientMap = new();
    }

    private static ServerSideConnectRequestManager instance;
    public static ServerSideConnectRequestManager Instance
    {
        get
        {
            if (instance == null)
            {
                ServerSideConnectRequestManager instanceInScene = GameObjectUtils.FindComponentWithTag<ServerSideConnectRequestManager>("ServerSideConnectRequestManager");
                if (instanceInScene != null)
                {
                    instanceInScene.InitSingleInstance();
                }
            }
            return instance;
        }
    }

    private static Dictionary<string, IConnectedClientHandler> idToConnectedClientMap = new();
    public static int ConnectedClientCount => idToConnectedClientMap.Count;
    
    private readonly Subject<ClientConnectionEvent> clientConnectedEventStream = new();
    public IObservable<ClientConnectionEvent> ClientConnectedEventStream => clientConnectedEventStream.ObserveOnMainThread();

    private UdpClient serverUdpClient;
    private const int ConnectPortOnServer = 34567;
    private const int ConnectPortOnClient = 34568;

    private bool hasBeenDestroyed;

    [Inject]
    private HttpServer httpServer;

    [Inject]
    private Settings settings;
    
    private void Start()
    {
        InitSingleInstance();
        if (!Application.isPlaying || instance != this)
        {
            return;
        }
        
        GameObjectUtils.SetTopLevelGameObjectAndDontDestroyOnLoad(gameObject);
        
        serverUdpClient = new UdpClient(ConnectPortOnServer);
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            while (!hasBeenDestroyed)
            {
                ServerAcceptMessageFromClient();
            }
        });
    }

    private void Update()
    {
        // Read messages from client that arrived since the last time the reader thread was active.
        GetConnectedClientHandlers().ForEach(connectedClientHandler => connectedClientHandler.ReadMessagesFromClient());
    }

    private void InitSingleInstance()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (instance != null
            && instance != this)
        {
            // This instance is not needed.
            Destroy(gameObject);
            return;
        }
        instance = this;
            
        // Move object to top level in scene hierarchy.
        // Otherwise this object will be destroyed with its parent, even when DontDestroyOnLoad is used.
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void ServerAcceptMessageFromClient()
    {
        try
        {
            Debug.Log("Server listening for connect request on " + serverUdpClient.GetPort());
            IPEndPoint clientIpEndPoint = new(IPAddress.Any, 0);
            // Receive is a blocking call.
            byte[] receivedBytes = serverUdpClient.Receive(ref clientIpEndPoint);
            string message = Encoding.UTF8.GetString(receivedBytes);
            HandleConnectRequest(clientIpEndPoint, message);
        }
        catch (Exception e)
        {
            if (e is SocketException se
                && se.SocketErrorCode == SocketError.Interrupted
                && hasBeenDestroyed)
            {
                // Dont log error when closing the socket has interrupted the wait for requests.
                return;
            }
            Debug.LogException(e);
        }
    }

    private void HandleConnectRequest(IPEndPoint clientIpEndPoint, string message)
    {
        Debug.Log($"Received connect request from client {clientIpEndPoint} ({clientIpEndPoint.Address}): '{message}'");
        try
        {
            ConnectRequestDto connectRequestDto = JsonConverter.FromJson<ConnectRequestDto>(message);
            if (connectRequestDto.ProtocolVersion != ProtocolVersions.ProtocolVersion)
            {
                throw new ConnectRequestException($"Malformed ConnectRequest: protocolVersion does not match"
                    + $" (server (main game): {ProtocolVersions.ProtocolVersion}, client (companion app): {connectRequestDto.ProtocolVersion}).");
            }
            if (connectRequestDto.ClientName.IsNullOrEmpty())
            {
                throw new ConnectRequestException("Malformed ConnectRequest: missing ClientName.");
            }
            if (connectRequestDto.ClientId.IsNullOrEmpty())
            {
                throw new ConnectRequestException("Malformed ConnectRequest: missing ClientId.");
            }

            HandleClientMessage(clientIpEndPoint, connectRequestDto);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            serverUdpClient.Send(new ConnectResponseDto
            {
                ErrorMessage = e.Message
            }.ToJson(), clientIpEndPoint);
        }
    }

    private void HandleClientMessageWithNoMicrophone(IPEndPoint clientIpEndPoint, ConnectRequestDto connectRequestDto)
    {
        ConnectResponseDto connectResponseDto = new()
        {
            ClientName = connectRequestDto.ClientName,
            ClientId = connectRequestDto.ClientId,
            HttpServerPort = httpServer.port,
        };
        serverUdpClient.Send(connectResponseDto.ToJson(), clientIpEndPoint);
    }

    private void HandleClientMessage(IPEndPoint clientIpEndPoint, ConnectRequestDto connectRequestDto)
    {
        ConnectedClientHandler newConnectedClientHandler = RegisterClient(clientIpEndPoint, connectRequestDto.ClientName, connectRequestDto.ClientId);
        clientConnectedEventStream.OnNext(new ClientConnectionEvent(newConnectedClientHandler, true));

        ConnectResponseDto connectResponseDto = new()
        {
            ClientName = connectRequestDto.ClientName,
            ClientId = connectRequestDto.ClientId,
            HttpServerPort = httpServer.port,
            MessagingPort = newConnectedClientHandler.ClientTcpListener.GetPort(),
        };
        Debug.Log("Sending ConnectResponse to " + clientIpEndPoint.Address + ":" + clientIpEndPoint.Port);
        serverUdpClient.Send(connectResponseDto.ToJson(), clientIpEndPoint);

        // Send MicProfile
        MicProfile micProfileOfClient = settings.MicProfiles
            .FirstOrDefault(micProfile => micProfile.ConnectedClientId == newConnectedClientHandler.ClientId);
        if (micProfileOfClient == null)
        {
            micProfileOfClient = new MicProfile();
        }

        Debug.Log("Sending MicProfile to " + clientIpEndPoint.Address + ":" + clientIpEndPoint.Port);
        newConnectedClientHandler.SendMessageToClient(new MicProfileMessageDto(micProfileOfClient));
    }

    private void OnDestroy()
    {
        hasBeenDestroyed = true;
        serverUdpClient?.Close();
        if (instance == this)
        {
            RemoveAllConnectedClientHandlers();
        }
    }
    
    private void RemoveAllConnectedClientHandlers()
    {
        idToConnectedClientMap.Values.ForEach(connectedClientHandler =>
        {
            clientConnectedEventStream.OnNext(new ClientConnectionEvent(connectedClientHandler, false));
            connectedClientHandler.Dispose();
        });
        idToConnectedClientMap.Clear();
    }

    public void RemoveConnectedClientHandler(IConnectedClientHandler connectedClientHandler)
    {
        if (idToConnectedClientMap.ContainsKey(connectedClientHandler.ClientId))
        {
            idToConnectedClientMap.Remove(connectedClientHandler.ClientId);
        }
        clientConnectedEventStream.OnNext(new ClientConnectionEvent(connectedClientHandler, false));
        connectedClientHandler.Dispose();
    }
    
    private ConnectedClientHandler RegisterClient(
        IPEndPoint clientIpEndPoint,
        string clientName,
        string clientId)
    {
        // Dispose any currently registered client with the same IP-Address.
        if (idToConnectedClientMap.TryGetValue(clientId, out IConnectedClientHandler existingConnectedClientHandler))
        {
            existingConnectedClientHandler.Dispose();
        }
        
        ConnectedClientHandler connectedClientHandler = new(this, clientIpEndPoint, clientName, clientId);
        idToConnectedClientMap[clientId] = connectedClientHandler;

        Debug.Log("New number of connected clients: " + idToConnectedClientMap.Count);
        
        return connectedClientHandler;
    }

    public static List<IConnectedClientHandler> GetConnectedClientHandlers()
    {
        return idToConnectedClientMap.Values.ToList();
    }
    
    public bool TryGetConnectedClientHandler(string clientId, out IConnectedClientHandler connectedClientHandler)
    {
        if (clientId == null)
        {
            connectedClientHandler = null;
            return false;
        }
        return idToConnectedClientMap.TryGetValue(clientId, out connectedClientHandler);
    }
}
