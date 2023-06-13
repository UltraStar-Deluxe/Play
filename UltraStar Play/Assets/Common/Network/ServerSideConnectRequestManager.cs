using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using SimpleHttpServerForUnity;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ServerSideConnectRequestManager : AbstractSingletonBehaviour, INeedInjection, IServerSideConnectRequestManager, INetEventListener, INetLogger
{
    public static ServerSideConnectRequestManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<ServerSideConnectRequestManager>();

    public int ConnectedClientCount => liteNetLibServer.ConnectedPeersCount;
    
    private readonly Subject<ClientConnectionEvent> clientConnectedEventStream = new();
    public IObservable<ClientConnectionEvent> ClientConnectedEventStream => clientConnectedEventStream.ObserveOnMainThread();
    
    private readonly Subject<MicProfile> connectedClientMicProfileChangedEventStream = new();
    public IObservable<MicProfile> ConnectedClientMicProfileChangedEventStream => connectedClientMicProfileChangedEventStream;
    
    [Inject]
    private HttpServer httpServer;

    [Inject]
    private Settings settings;

    private NetManager liteNetLibServer;
    private NetPeer liteNetLibPeer;

    private readonly Dictionary<NetPeer, IConnectedClientHandler> peerToConnectedClientHandler = new();
    private readonly Dictionary<NetPeer, ConnectRequestDto> peerToConnectRequestDto = new();

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        if (!Application.isPlaying || Instance != this)
        {
            return;
        }

        NetDebug.Logger = this;
        liteNetLibServer = new NetManager(this);
        liteNetLibServer.BroadcastReceiveEnabled = true;
        // 16 ms are approx. 60 FPS
        liteNetLibServer.UpdateTime = 16;
        liteNetLibServer.Start(settings.IpPortOnServer);
        Debug.Log($"Listening for broadcast messages on port {settings.IpPortOnServer}");

        ClientConnectedEventStream
            .Subscribe(evt => UpdateConnectedMicProfileName(evt));
    }

    private void OnDestroy()
    {
        NetDebug.Logger = null;
        liteNetLibServer?.Stop();
        
        if (Instance == this)
        {
            RemoveAllConnectedClientHandlers();
        }
    }
    

    private void Update()
    {
        liteNetLibServer.PollEvents();
    }
    
    public void UpdateConnectedMicProfileName(ClientConnectionEvent clientConnectionEvent)
    {
        if (!clientConnectionEvent.IsConnected)
        {
            return;
        }
        
        settings.MicProfiles
            .ForEach(micProfile =>
            {
                if (micProfile.IsInputFromConnectedClient
                    && micProfile.ConnectedClientId == clientConnectionEvent.ConnectedClientHandler.ClientId
                    && micProfile.Name != clientConnectionEvent.ConnectedClientHandler.ClientName)
                {
                    micProfile.Name = clientConnectionEvent.ConnectedClientHandler.ClientName;
                    connectedClientMicProfileChangedEventStream.OnNext(micProfile);
                }
            });
    }
    
    private void RemoveAllConnectedClientHandlers()
    {
        foreach (NetPeer peer in liteNetLibServer.ConnectedPeerList.ToList())
        {
            peer.Disconnect();
        }
    }

    private ConnectedClientHandler RegisterConnectedClient(
        NetPeer peer,
        string clientName,
        string clientId)
    {
        ConnectedClientHandler connectedClientHandler = new(peer, clientName, clientId);
        peerToConnectedClientHandler[peer] = connectedClientHandler;
        return connectedClientHandler;
    }

    public List<IConnectedClientHandler> GetAllConnectedClientHandlers()
    {
        return peerToConnectedClientHandler.Values.ToList();
    }

    public bool TryGetConnectedClientHandler(string clientId, out IConnectedClientHandler connectedClientHandler)
    {
        if (clientId == null)
        {
            connectedClientHandler = null;
            return false;
        }

        connectedClientHandler = peerToConnectedClientHandler.Values.FirstOrDefault(it => it.ClientId == clientId);
        return connectedClientHandler != null;
    }

    public List<ConnectedClientHandlerAndMicProfile> GetConnectedClientHandlers(IEnumerable<MicProfile> micProfiles)
    {
        List<ConnectedClientHandlerAndMicProfile> result = new();
        micProfiles
            .Where(micProfile => micProfile != null && micProfile.IsInputFromConnectedClient)
            .ForEach(micProfile =>
            {
                if (TryGetConnectedClientHandler(micProfile.ConnectedClientId, out IConnectedClientHandler connectedClientHandler))
                {
                    result.Add(new ConnectedClientHandlerAndMicProfile(connectedClientHandler, micProfile));
                }
            });
        return result;
    }

    public void OnPeerConnected(NetPeer peer)
    {
        if (!peerToConnectRequestDto.TryGetValue(peer, out ConnectRequestDto connectRequestDto))
        {
            Debug.LogError($"Peer connected without ConnectRequest data: {peer.EndPoint}");
            return;
        }
        
        Debug.Log($"Peer connected {peer.EndPoint} with ConnectRequest: {connectRequestDto.ToJson()}. Sending ConnectResponse.");
        
        // Send connect response
        List<HttpApiPermission> permissions = SettingsUtils.GetPermissions(settings, connectRequestDto.ClientId);
        ConnectResponseDto connectResponseDto = new()
        {
            ClientName = connectRequestDto.ClientName,
            ClientId = connectRequestDto.ClientId,
            HttpServerPort = httpServer.port,
            Permissions = permissions,
        };
        Debug.Log($"Sending ConnectResponse to {peer.EndPoint}");
        peer.Send(connectResponseDto, DeliveryMethod.ReliableOrdered);
        
        // Send MicProfile
        MicProfile micProfileOfClient = settings.MicProfiles
            .FirstOrDefault(micProfile => micProfile.ConnectedClientId == connectRequestDto.ClientId);
        if (micProfileOfClient == null)
        {
            micProfileOfClient = new MicProfile();
        }

        Debug.Log($"Sending MicProfile to {peer.EndPoint}");
        peer.Send(new MicProfileMessageDto(micProfileOfClient), DeliveryMethod.ReliableOrdered);
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.LogWarning($"Peer disconnected: {peer.EndPoint}, reason: {disconnectInfo.Reason}");
        if (!peerToConnectedClientHandler.TryGetValue(peer, out IConnectedClientHandler connectedClientHandler))
        {
            return;
        }

        peerToConnectedClientHandler.Remove(peer);
        clientConnectedEventStream.OnNext(new ClientConnectionEvent(connectedClientHandler, false));
    }

    public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
    {
        Debug.LogError($"Network error: {endPoint}, {socketError}");
    }

    public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
    {
        string message = reader.GetString();
        if (message.IsNullOrEmpty())
        {
            return;
        }
        
        // Debug.Log($"Received message from client {peer.EndPoint}: {message}");
        
        if (peerToConnectedClientHandler.TryGetValue(peer, out IConnectedClientHandler connectedClientHandler))
        {
            connectedClientHandler.HandleMessageFromClient(message);
        }
        else
        {
            Debug.LogError($"Received message from unknown peer {peer.EndPoint}: {message}");
        }
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (messageType is UnconnectedMessageType.Broadcast)
        {
            Debug.Log($"Received discovery request from {remoteEndPoint}. Sending discovery response.");
            NetDataWriter netDataWriter = new();
            netDataWriter.Put(1);
            liteNetLibServer.SendUnconnectedMessage(netDataWriter, remoteEndPoint);
        }
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        // Debug.Log($"OnNetworkLatencyUpdate: {peer.EndPoint}, latency: {latency}");
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
        IPEndPoint remoteEndPoint = request.RemoteEndPoint;
        if (request.Data.IsNull)
        {
            Debug.LogError("Missing connection request");
            return;
        }
        
        string message = request.Data.GetString();
        if (message.IsNullOrEmpty())
        {
            Debug.LogError("Missing connection request");
            return;
        }

        try
        {
            if (!CompanionAppMessageUtils.TryGetMessageType(message, out CompanionAppMessageType messageType))
            {
                throw new Exception($"Malformed connection request: wrong message type");
            }
            
            ConnectRequestDto connectRequestDto = JsonConverter.FromJson<ConnectRequestDto>(message);
            if (connectRequestDto.ProtocolVersion != ProtocolVersions.ProtocolVersion)
            {
                throw new ConnectRequestException($"Malformed connection request: protocol version does not match"
                                                  + $" (server (main game): {ProtocolVersions.ProtocolVersion}, client (companion app): {connectRequestDto.ProtocolVersion}).");
            }
            if (connectRequestDto.ClientName.IsNullOrEmpty())
            {
                throw new ConnectRequestException($"Malformed connection request: missing ClientName.");
            }
            if (connectRequestDto.ClientId.IsNullOrEmpty())
            {
                throw new ConnectRequestException($"Malformed connection request: missing ClientId.");
            }
            
            Debug.Log($"Accepted connection request from {remoteEndPoint}");
            NetPeer peer = request.Accept();
            
            // Register client
            peerToConnectRequestDto[peer] = connectRequestDto;
            ConnectedClientHandler newConnectedClientHandler = RegisterConnectedClient(peer, connectRequestDto.ClientName, connectRequestDto.ClientId);
            clientConnectedEventStream.OnNext(new ClientConnectionEvent(newConnectedClientHandler, true));
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Received invalid ConnectRequest from {remoteEndPoint}: {message}");

            NetDataWriter netDataWriter = new();
            netDataWriter.Put(new ConnectResponseDto()
            {
                ErrorMessage = e.Message,
            }.ToJson());
            request.Reject(netDataWriter);
        }
    }

    public void WriteNet(NetLogLevel level, string str, params object[] args)
    {
        Debug.LogFormat(level.ToUnityLogType(), LogOption.NoStacktrace, this, str, args);
    }
}
