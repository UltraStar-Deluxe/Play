using System;
using System.Net;
using System.Net.Sockets;
using LiteNetLib;
using LiteNetLib.Utils;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ClientSideCompanionClientManager : AbstractSingletonBehaviour, INeedInjection, INetEventListener
{
    public static ClientSideCompanionClientManager Instance => DontDestroyOnLoadManager.FindComponentOrThrow<ClientSideCompanionClientManager>();

    [Inject]
    private Settings settings;

    private float nextConnectRequestTime;

    private readonly Subject<ConnectEvent> connectEventStream = new Subject<ConnectEvent>();
    public IObservable<ConnectEvent> ConnectEventStream => connectEventStream;

    private bool isListeningForConnectResponse;

    private bool hasBeenDestroyed;

    private int connectRequestCount;

    private NetPeer ServerPeer => liteNetLibClient.FirstPeer;
    private bool HasServerPeer => ServerPeer != null;

    public bool IsConnected => HasServerPeer
                               && ServerPeer.ConnectionState is ConnectionState.Connected;

    private readonly Subject<JsonSerializable> receivedMessageStream = new();
    public IObservable<JsonSerializable> ReceivedMessageStream => receivedMessageStream;

    private NetManager liteNetLibClient;

    private float lastConnectionAttemptTimeInSeconds;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        liteNetLibClient = new NetManager(this);
        liteNetLibClient.UnconnectedMessagesEnabled = true;
        // 16 ms are approx. 60 FPS
        liteNetLibClient.UpdateTime = 16;
        StartLiteNetLibClient();
    }

    protected override void OnDestroySingleton()
    {
        hasBeenDestroyed = true;
        if (liteNetLibClient != null)
        {
            liteNetLibClient.Stop();
        }
    }

    private void ConnectToServer()
    {
        if (HasServerPeer)
        {
            return;
        }

        connectEventStream.OnNext(new ConnectEvent(connectRequestCount));
        connectRequestCount++;

        if (settings.ConnectionServerAddress.IsNullOrEmpty()
            || settings.ConnectionServerPort <= 0)
        {
            SendDiscoveryBroadcast();
        }
        else
        {
            try
            {
                Debug.Log($"Attempt to connect with manually set host {settings.ConnectionServerAddress} and port {settings.ConnectionServerPort}");
                IPAddress ipAddress = IPAddress.Parse(settings.ConnectionServerAddress);
                SendConnectMessage(new IPEndPoint(ipAddress, settings.ConnectionServerPort));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                Debug.LogError($"Failed forced connection to server with host {settings.ConnectionServerAddress} and port {settings.ConnectionServerPort}: {e.Message}");
            }
        }
    }

    private void SendDiscoveryBroadcast()
    {
        Debug.Log($"Sending discovery broadcast to port {settings.ConnectionServerPort}. Connect request count: {connectRequestCount}");

        NetDataWriter netDataWriter = new NetDataWriter();
        netDataWriter.Put(1);
        liteNetLibClient.SendBroadcast(netDataWriter, settings.ConnectionServerPort);
    }

    private void OnReceivedDiscoveryResponse(IPEndPoint remoteEndPoint)
    {
        Debug.Log($"Received discovery response from {remoteEndPoint}");
        SendConnectMessage(remoteEndPoint);
    }

    private void SendConnectMessage(IPEndPoint remoteEndPoint)
    {
        Debug.Log($"Sending connect request to {remoteEndPoint}");
        ConnectRequestDto connectRequestDto = new()
        {
            ClientId = settings.ClientId,
            ClientName = settings.ClientName,
            ProtocolVersion = CompanionClientProtocolVersion.ProtocolVersion
        };
        NetDataWriter netDataWriter = new NetDataWriter();
        netDataWriter.Put(connectRequestDto.ToJson());
        liteNetLibClient.Connect(remoteEndPoint, netDataWriter);
    }

    public void DisconnectFromServer()
    {
        if (ServerPeer == null)
        {
            return;
        }

        ServerPeer?.Disconnect();
        connectRequestCount = 0;
        connectEventStream.OnNext(new ConnectEvent(connectRequestCount));
    }

    private void StartLiteNetLibClient()
    {
        if (liteNetLibClient == null
            || liteNetLibClient.IsRunning)
        {
            return;
        }

        Debug.Log($"Starting {nameof(liteNetLibClient)}");
        liteNetLibClient.Start();
        Debug.Log($"Started {nameof(liteNetLibClient)} on port {liteNetLibClient.LocalPort}");
    }

    private void StopLiteNetLibClient()
    {
        if (liteNetLibClient == null
            || !liteNetLibClient.IsRunning)
        {
            return;
        }

        Debug.Log($"Stopping {nameof(liteNetLibClient)}");
        liteNetLibClient.Stop(true);
    }

    private void Update()
    {
        liteNetLibClient.PollEvents();

        // Try to connect to the server every second.
        if (!HasServerPeer
            && (lastConnectionAttemptTimeInSeconds == 0
                || Time.time - lastConnectionAttemptTimeInSeconds > 1))
        {
            lastConnectionAttemptTimeInSeconds = Time.time;
            ConnectToServer();
        }
    }

    private void OnApplicationPause(bool isPaused)
    {
        if (Application.isEditor
            || PlatformUtils.IsStandalone)
        {
            Log.Verbose(() => $"OnApplicationPause: ignoring because not running on mobile device.");
            return;
        }

        Log.Debug(() => $"OnApplicationPause: isPaused: {isPaused}");

        if (isPaused)
        {
            // Application is paused now (e.g. the app was moved to the background on Android).
            // Stop LiteNetLib because iOS may close the socket that was used for the connection.
            StopLiteNetLibClient();
        }
        else
        {
            // Application was resumed. Need to start LiteNetLibClient again, possibly on different port.
            StartLiteNetLibClient();
        }
    }

    public void SendMessageToServer(JsonSerializable jsonSerializable, DeliveryMethod deliveryMethod)
    {
        if (jsonSerializable == null
            || !HasServerPeer)
        {
            return;
        }

        ServerPeer.Send(jsonSerializable, deliveryMethod);
    }

    private void HandleMessageFromServer(string message)
    {
        message = message.Trim();
        if (!message.StartsWith("{")
            || !message.EndsWith("}"))
        {
            Debug.LogWarning($"Received invalid message from server: {message}");
            return;
        }

        Log.Verbose(() => $"Received message from server: {message}");
        HandleJsonMessageFromServer(message);
    }

    private void HandleJsonMessageFromServer(string json)
    {
        if (!CompanionAppMessageUtils.TryGetMessageType(json, out CompanionAppMessageType messageType))
        {
            Debug.LogWarning($"Received message with invalid type from server: {json}");
            return;
        }

        switch (messageType)
        {
            case CompanionAppMessageType.ConnectResponse:
                try
                {
                    HandleConnectResponse(json);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    Debug.LogError("Failed to handle connect response. Disconnecting from server.");
                    DisconnectFromServer();

                    connectEventStream.OnNext(new ConnectEvent(connectRequestCount, e.Message));
                }
                return;
            case CompanionAppMessageType.PositionInSong:
                receivedMessageStream.OnNext(JsonConverter.FromJson<PositionInSongDto>(json));
                return;
            case CompanionAppMessageType.MicProfile:
                receivedMessageStream.OnNext(JsonConverter.FromJson<MicProfileMessageDto>(json));
                return;
            case CompanionAppMessageType.StopRecording:
                receivedMessageStream.OnNext(JsonConverter.FromJson<StopRecordingMessageDto>(json));
                return;
            case CompanionAppMessageType.StartRecording:
                receivedMessageStream.OnNext(JsonConverter.FromJson<StartRecordingMessageDto>(json));
                return;
            case CompanionAppMessageType.Permissions:
                receivedMessageStream.OnNext(JsonConverter.FromJson<PermissionsMessageDto>(json));
                return;
            default:
                Debug.Log($"Unknown MessageType {messageType} in JSON from server: {json}");
                return;
        }
    }

    private void HandleConnectResponse(string message)
    {
        ConnectResponseDto connectResponseDto = JsonConverter.FromJson<ConnectResponseDto>(message);
        if (!connectResponseDto.ErrorMessage.IsNullOrEmpty())
        {
            throw new ConnectRequestException("Received error message: " + connectResponseDto.ErrorMessage);
        }
        if (connectResponseDto.ClientName.IsNullOrEmpty())
        {
            throw new ConnectRequestException("Malformed ConnectResponse: missing ClientName.");
        }
        if (connectResponseDto.ClientId.IsNullOrEmpty())
        {
            throw new ConnectRequestException("Malformed ConnectResponse: missing ClientId.");
        }
        if (!string.Equals(connectResponseDto.ClientId, settings.ClientId, StringComparison.InvariantCulture))
        {
            throw new ConnectRequestException($"Malformed ConnectResponse: wrong ClientId. Is {connectResponseDto.ClientId}, expected {settings.ClientId}");
        }

        connectEventStream.OnNext(new ConnectEvent(
            connectResponseDto.HttpServerPort,
            ServerPeer,
            connectResponseDto.Permissions,
            connectResponseDto.AvailableGameRoundModifierDtos));
        connectRequestCount = 0;
    }

    public void OnPeerConnected(NetPeer peer)
    {
        Debug.Log($"Connected to {peer}");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        string disconnectInfoAdditionalData;
        try
        {
            disconnectInfoAdditionalData = disconnectInfo.AdditionalData.GetString();
        }
        catch (Exception ex)
        {
            ex.Log("Failed to read additional info from disconnect message");
            disconnectInfoAdditionalData = "";
        }
        Debug.Log($"Disconnected: reason: {disconnectInfo.Reason}, additional info: {disconnectInfoAdditionalData}, socket error code: {disconnectInfo.SocketErrorCode}");

        if (disconnectInfoAdditionalData.Trim().StartsWith("{")
            && disconnectInfoAdditionalData.Trim().EndsWith("}")
            && disconnectInfoAdditionalData.Contains("ErrorMessage", StringComparison.InvariantCultureIgnoreCase))
        {
            HandleMessageFromServer(disconnectInfoAdditionalData);
        }
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

        HandleMessageFromServer(message);
    }

    public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
    {
        if (messageType is UnconnectedMessageType.BasicMessage
            && liteNetLibClient.ConnectedPeersCount == 0
            && reader.GetInt() == 1)
        {
            OnReceivedDiscoveryResponse(remoteEndPoint);
        }
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
    }

    public void OnConnectionRequest(ConnectionRequest request)
    {
    }
}
