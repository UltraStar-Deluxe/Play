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

public class ServerSideCompanionClientManager : AbstractSingletonBehaviour, INeedInjection, IServerSideCompanionClientManager, INetEventListener
{
    public static ServerSideCompanionClientManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<ServerSideCompanionClientManager>();

    public int CompanionClientCount => liteNetLibServer.ConnectedPeersCount;

    private readonly Subject<CompanionClientConnectionChangedEvent> clientConnectionChangedEventStream = new();
    public IObservable<CompanionClientConnectionChangedEvent> ClientConnectionChangedEventStream => clientConnectionChangedEventStream.ObserveOnMainThread();

    private readonly Subject<MicProfile> companionClientMicProfileChangedEventStream = new();
    public IObservable<MicProfile> CompanionClientMicProfileChangedEventStream => companionClientMicProfileChangedEventStream;

    [Inject]
    private HttpServer httpServer;

    [Inject]
    private Settings settings;

    private NetManager liteNetLibServer;
    private NetPeer liteNetLibPeer;

    private readonly Dictionary<NetPeer, ICompanionClientHandler> peerToCompanionClientHandler = new();
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

        NetDebug.Logger = new CustomNetLogger(gameObject);
        liteNetLibServer = new NetManager(this);
        liteNetLibServer.BroadcastReceiveEnabled = true;
        // 16 ms are approx. 60 FPS
        liteNetLibServer.UpdateTime = 16;
        liteNetLibServer.Start(settings.ConnectionServerPort);
        Debug.Log($"Listening for broadcast messages on port {settings.ConnectionServerPort}");

        ClientConnectionChangedEventStream
            .Subscribe(evt => OnClientConnectionChanged(evt));
    }

    private void OnDestroy()
    {
        NetDebug.Logger = null;
        liteNetLibServer?.Stop();

        if (Instance == this)
        {
            RemoveAllCompanionClientHandlers();
        }
    }


    private void Update()
    {
        liteNetLibServer.PollEvents();
    }

    public void OnClientConnectionChanged(CompanionClientConnectionChangedEvent companionClientConnectionChangedEvent)
    {
        if (!companionClientConnectionChangedEvent.IsConnected)
        {
            return;
        }

        settings.MicProfiles
            .ForEach(micProfile =>
            {
                if (micProfile.IsInputFromConnectedClient
                    && micProfile.ConnectedClientId == companionClientConnectionChangedEvent.CompanionClientHandler.ClientId
                    && micProfile.Name != companionClientConnectionChangedEvent.CompanionClientHandler.ClientName)
                {
                    micProfile.Name = companionClientConnectionChangedEvent.CompanionClientHandler.ClientName;
                    companionClientMicProfileChangedEventStream.OnNext(micProfile);
                }
            });
    }

    private void RemoveAllCompanionClientHandlers()
    {
        foreach (NetPeer peer in liteNetLibServer.ConnectedPeerList.ToList())
        {
            peer.Disconnect();
        }
    }

    private CompanionClientHandler RegisterCompanionClient(
        NetPeer peer,
        string clientName,
        string clientId)
    {
        CompanionClientHandler companionClientHandler = new(peer, clientName, clientId);
        peerToCompanionClientHandler[peer] = companionClientHandler;
        return companionClientHandler;
    }

    public void DisconnectAll()
    {
        liteNetLibServer.DisconnectAll();
    }

    public List<ICompanionClientHandler> GetAllCompanionClientHandlers()
    {
        return peerToCompanionClientHandler.Values.ToList();
    }

    public bool TryGet(string clientId, out ICompanionClientHandler companionClientHandler)
    {
        if (clientId == null)
        {
            companionClientHandler = null;
            return false;
        }

        companionClientHandler = peerToCompanionClientHandler.Values.FirstOrDefault(it => it.ClientId == clientId);
        return companionClientHandler != null;
    }

    public List<CompanionClientHandlerAndMicProfile> GetCompanionClientHandlers(IEnumerable<MicProfile> micProfiles)
    {
        List<CompanionClientHandlerAndMicProfile> result = new();
        micProfiles
            .Where(micProfile => micProfile != null && micProfile.IsInputFromConnectedClient)
            .ForEach(micProfile =>
            {
                if (TryGet(micProfile.ConnectedClientId, out ICompanionClientHandler companionClientHandler))
                {
                    result.Add(new CompanionClientHandlerAndMicProfile(companionClientHandler, micProfile));
                }
            });
        return result;
    }

    public void OnPeerConnected(NetPeer peer)
    {
        if (!peerToConnectRequestDto.TryGetValue(peer, out ConnectRequestDto connectRequestDto))
        {
            Debug.LogError($"Peer connected without ConnectRequest data: {peer}");
            return;
        }

        Debug.Log($"Peer connected {peer} with ConnectRequest: {connectRequestDto.ToJson()}. Sending ConnectResponse.");

        // Send connect response
        List<RestApiPermission> permissions = SettingsUtils.GetPermissions(settings, connectRequestDto.ClientId);
        List<GameRoundModifierDto> availableGameRoundModifierDtos = DtoConverter.ToDto(GameRoundModifierRegistry.GetAll());
        ConnectResponseDto connectResponseDto = new()
        {
            ClientName = connectRequestDto.ClientName,
            ClientId = connectRequestDto.ClientId,
            HttpServerPort = httpServer.port,
            Permissions = permissions,
            AvailableGameRoundModifierDtos = availableGameRoundModifierDtos,
        };
        Debug.Log($"Sending ConnectResponse to {peer}");
        peer.Send(connectResponseDto, DeliveryMethod.ReliableOrdered);

        // Send MicProfile
        MicProfile micProfileOfClient = GetOrCreateMicProfile(connectRequestDto);
        Debug.Log($"Sending MicProfile to {peer}");
        peer.Send(new MicProfileMessageDto(micProfileOfClient), DeliveryMethod.ReliableOrdered);
    }

    private MicProfile GetOrCreateMicProfile(ConnectRequestDto connectRequestDto)
    {
        MicProfile micProfileOfClient = settings.MicProfiles
            .FirstOrDefault(micProfile => micProfile.ConnectedClientId == connectRequestDto.ClientId);
        if (micProfileOfClient == null)
        {
            micProfileOfClient = new MicProfile(connectRequestDto.ClientName, 0, connectRequestDto.ClientId);
            micProfileOfClient.Color = ColorGenerationUtils.FromString(Guid.NewGuid().ToString());
            settings.MicProfiles.Add(micProfileOfClient);
        }

        return micProfileOfClient;
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.LogWarning($"Peer disconnected: {peer}, reason: {disconnectInfo.Reason}");
        if (!peerToCompanionClientHandler.TryGetValue(peer, out ICompanionClientHandler companionClientHandler))
        {
            return;
        }

        peerToCompanionClientHandler.Remove(peer);
        clientConnectionChangedEventStream.OnNext(new CompanionClientConnectionChangedEvent(companionClientHandler, false));
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

        Log.Verbose(() => $"Received message from client {peer}: {message}");

        if (peerToCompanionClientHandler.TryGetValue(peer, out ICompanionClientHandler companionClientHandler))
        {
            companionClientHandler.HandleMessageFromClient(message);
        }
        else
        {
            Debug.LogError($"Received message from unknown peer {peer}: {message}");
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
        Log.Verbose(() => $"OnNetworkLatencyUpdate: {peer}, latency: {latency}");
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
            if (connectRequestDto.ProtocolVersion != CompanionClientProtocolVersion.ProtocolVersion)
            {
                throw new ConnectRequestException($"Malformed connection request: protocol version does not match"
                                                  + $" (server (main game): {CompanionClientProtocolVersion.ProtocolVersion}, client (companion app): {connectRequestDto.ProtocolVersion}).");
            }

            if (connectRequestDto.ClientId.IsNullOrEmpty())
            {
                throw new ConnectRequestException($"Malformed connection request: missing ClientId.");
            }

            Debug.Log($"Accepted connection request from {remoteEndPoint}");
            NetPeer peer = request.Accept();

            // Set default values
            if (connectRequestDto.ClientName.IsNullOrEmpty())
            {
                // ClientName must not be empty
                connectRequestDto.ClientName = "Companion App";
            }

            // Register client
            peerToConnectRequestDto[peer] = connectRequestDto;
            CompanionClientHandler newCompanionClientHandler = RegisterCompanionClient(peer, connectRequestDto.ClientName, connectRequestDto.ClientId);
            clientConnectionChangedEventStream.OnNext(new CompanionClientConnectionChangedEvent(newCompanionClientHandler, true));
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

    public IPEndPoint GetConnectionEndpoint()
    {
        IPAddress localIpAddress = IpAddressUtils.GetLocalIpAddress();
        return new IPEndPoint(localIpAddress, liteNetLibServer.LocalPort);
    }

    public class CustomNetLogger : INetLogger
    {
        public static bool ErrorToWarning { get; set; }

        private readonly GameObject context;

        public CustomNetLogger(GameObject context)
        {
            this.context = context;
        }

        public void WriteNet(NetLogLevel level, string str, params object[] args)
        {
            if (ErrorToWarning
                && level == NetLogLevel.Error)
            {
                level = NetLogLevel.Warning;
            }

            Debug.LogFormat(level.ToUnityLogType(), LogOption.NoStacktrace, context, str, args);
        }
    }
}
