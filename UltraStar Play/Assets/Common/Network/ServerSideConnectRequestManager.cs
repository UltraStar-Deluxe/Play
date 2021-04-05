using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SimpleHttpServerForUnity;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEditor;
using AddressFamily = SimpleHttpServerForUnity.AddressFamily;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ServerSideConnectRequestManager : MonoBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void InitOnLoad()
    {
        instance = null;
        idToConnectedClientMap = new Dictionary<string, ConnectedClientHandler>();
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

    private static Dictionary<string, ConnectedClientHandler> idToConnectedClientMap = new Dictionary<string, ConnectedClientHandler>();
    public static int ConnectedClientCount => idToConnectedClientMap.Count;
    
    private readonly ConcurrentQueue<ClientConnectionEvent> clientConnectedEventQueue = new ConcurrentQueue<ClientConnectionEvent>();
    
    private readonly Subject<ClientConnectionEvent> clientConnectedEventStream = new Subject<ClientConnectionEvent>();
    public IObservable<ClientConnectionEvent> ClientConnectedEventStream => clientConnectedEventStream;

    /**
     * This version number must to be increased when introducing breaking changes.
     */
    public const int ProtocolVersion = 1;
    
    private UdpClient serverUdpClient;
    private const int ConnectPortOnServer = 34567;
    private const int ConnectPortOnClient = 34568;

    private bool hasBeenDestroyed;

    [Inject]
    private HttpServer httpServer;
    
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
        // Fire events on the main thread.
        while (clientConnectedEventQueue.TryDequeue(out ClientConnectionEvent clientConnectedEvent))
        {
            clientConnectedEventStream.OnNext(clientConnectedEvent);
        }
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
            IPEndPoint clientIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            // Receive is a blocking call.
            byte[] receivedBytes = serverUdpClient.Receive(ref clientIpEndPoint);
            string message = Encoding.UTF8.GetString(receivedBytes);
            HandleClientMessage(clientIpEndPoint, message);
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

    private void HandleClientMessage(IPEndPoint clientIpEndPoint, string message)
    {
        Debug.Log($"Received message from client {clientIpEndPoint} ({clientIpEndPoint.Address}): '{message}'");
        try
        {
            ConnectRequestDto connectRequestDto = JsonConverter.FromJson<ConnectRequestDto>(message);
            if (connectRequestDto.ProtocolVersion != ProtocolVersion)
            {
                throw new ConnectRequestException($"Malformed ConnectRequest: protocolVersion does not match"
                    + $" (server (main game): {ProtocolVersion}, client (companion app): {connectRequestDto.ProtocolVersion}).");
            }
            if (connectRequestDto.ClientName.IsNullOrEmpty())
            {
                throw new ConnectRequestException("Malformed ConnectRequest: missing clientName.");
            }

            if (connectRequestDto.MicrophoneSampleRate > 0)
            {
                HandleClientMessageWithMicrophone(clientIpEndPoint, connectRequestDto);
            }
            else
            {
                HandleClientMessageWithNoMicrophone(clientIpEndPoint, connectRequestDto);
            }
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
        ConnectResponseDto connectResponseDto = new ConnectResponseDto
        {
            ClientName = connectRequestDto.ClientName,
            HttpServerPort = httpServer.port,
        };
        serverUdpClient.Send(connectResponseDto.ToJson(), clientIpEndPoint);
    }

    private void HandleClientMessageWithMicrophone(IPEndPoint clientIpEndPoint, ConnectRequestDto connectRequestDto)
    {
        ConnectedClientHandler newConnectedClientHandler = RegisterClient(clientIpEndPoint, connectRequestDto.ClientName, connectRequestDto.MicrophoneSampleRate);
        clientConnectedEventQueue.Enqueue(new ClientConnectionEvent(newConnectedClientHandler, true));
        
        ConnectResponseDto connectResponseDto = new ConnectResponseDto
        {
            ClientName = connectRequestDto.ClientName,
            HttpServerPort = httpServer.port,
            MicrophonePort = newConnectedClientHandler.MicTcpListener.GetPort(),
        };
        Debug.Log("Sending ConnectResponse to " + clientIpEndPoint.Address + ":" + clientIpEndPoint.Port);
        serverUdpClient.Send(connectResponseDto.ToJson(), clientIpEndPoint);
    }

    private void OnDestroy()
    {
        hasBeenDestroyed = true;
        serverUdpClient?.Close();
        if (instance == this)
        {
            RemoveAllConnectedClients();
        }
    }
    
    private void RemoveAllConnectedClients()
    {
        idToConnectedClientMap.Values.ForEach(connectedClientHandler =>
        {
            clientConnectedEventQueue.Enqueue(new ClientConnectionEvent(connectedClientHandler, false));
            connectedClientHandler.Dispose();
        });
        idToConnectedClientMap.Clear();
    }

    public void RemoveConnectedClientHandler(ConnectedClientHandler connectedClientHandler)
    {
        if (idToConnectedClientMap.ContainsKey(connectedClientHandler.ClientId))
        {
            idToConnectedClientMap.Remove(connectedClientHandler.ClientId);
        }
        clientConnectedEventQueue.Enqueue(new ClientConnectionEvent(connectedClientHandler, false));
        connectedClientHandler.Dispose();
    }
    
    private ConnectedClientHandler RegisterClient(
        IPEndPoint clientIpEndPoint,
        string clientName,
        int microphoneSampleRate)
    {
        // Dispose any currently registered client with the same IP-Address.
        if (idToConnectedClientMap.TryGetValue(GetClientId(clientIpEndPoint), out ConnectedClientHandler existingConnectedClientHandler))
        {
            existingConnectedClientHandler.Dispose();
        }
        
        ConnectedClientHandler connectedClientHandler = new ConnectedClientHandler(this, clientIpEndPoint, clientName, microphoneSampleRate);
        idToConnectedClientMap[GetClientId(clientIpEndPoint)] = connectedClientHandler;

        Debug.Log("New number of connected clients: " + idToConnectedClientMap.Count);
        
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
