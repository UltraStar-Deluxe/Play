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

public class UdpBroadcaster : MonoBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void InitOnLoad()
    {
        Instance = null;
    }
    
    public static UdpBroadcaster Instance { get; private set; }

    private UdpClient serverUdpClient;
    private UdpClient clientUdpClient;
    private const int ConnectPortOnServer = 34567;
    private const int ConnectPortOnClient = 34568;

    private string defaultClientId = "MyCompanionApp";

    private bool isListeningForConnectRequest;
    private bool isListeningForConnectResponse;

    private bool hasBeenDestroyed;
    
    private void Start()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        serverUdpClient = new UdpClient(ConnectPortOnServer);
        clientUdpClient = new UdpClient(ConnectPortOnClient);

        // Move object to top level in scene hierarchy.
        // Otherwise this object will be destroyed with its parent, even when DontDestroyOnLoad is used. 
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            ServerListenForConnectRequest();
        });
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            ClientListenForConnectResponse();
        });
        
        Thread.Sleep(100);
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            ClientSendConnectRequest();
        });
    }
    
    private void ServerListenForConnectRequest()
    {
        if (isListeningForConnectRequest)
        {
            Debug.LogError("Already listening for connect request.");
            return;
        }
        isListeningForConnectRequest = true;
    
        while (!hasBeenDestroyed)
        {
            ServerAcceptMessageFromClient();
        }
    }

    private void ServerAcceptMessageFromClient()
    {
        try
        {
            Debug.Log("Server listening for connect request on " + ConnectPortOnServer);
            IPEndPoint clientIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
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
        ConnectRequestDto connectRequestDto = JsonConverter.FromJson<ConnectRequestDto>(message);
        if (connectRequestDto.clientId.IsNullOrEmpty())
        {
            Debug.LogWarning("Malformed ConnectRequest: missing clientId.");
        }

        if (connectRequestDto.microphoneSampleRate > 0)
        {
            HandleClientMessageWithMicrophone(clientIpEndPoint, connectRequestDto);
        }
        else
        {
            HandleClientMessageWithNoMicrophone(clientIpEndPoint, connectRequestDto);
        }
    }

    private void HandleClientMessageWithNoMicrophone(IPEndPoint clientIpEndPoint, ConnectRequestDto connectRequestDto)
    {
        ConnectResponseDto connectResponseDto = new ConnectResponseDto
        {
            clientId = connectRequestDto.clientId,
        };
        serverUdpClient.Send(connectResponseDto.ToJson(), clientIpEndPoint);
    }

    private void HandleClientMessageWithMicrophone(IPEndPoint clientIpEndPoint, ConnectRequestDto connectRequestDto)
    {
        ConnectedClientHandler newConnectedClientHandler;
        try
        {
            newConnectedClientHandler = ClientConnectionManager.RegisterClient(connectRequestDto.clientId, connectRequestDto.microphoneSampleRate);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            serverUdpClient.Send(new ErrorMessageDto
            {
                errorMessage = e.Message
            }.ToJson(), clientIpEndPoint);
            return;
        }
        
        ConnectResponseDto connectResponseDto = new ConnectResponseDto
        {
            clientId = connectRequestDto.clientId,
            microphonePort = newConnectedClientHandler.MicrophoneUdpClient.GetPort(),
        };
        serverUdpClient.Send(connectResponseDto.ToJson(), clientIpEndPoint);
    }

    private void ClientListenForConnectResponse()
    {
        if (isListeningForConnectResponse)
        {
            Debug.LogError("Already listening for connect response");
            return;
        }
        isListeningForConnectResponse = true;
    
        while (!hasBeenDestroyed)
        {
            ClientAcceptMessageFromServer();
        }
    }

    private void ClientAcceptMessageFromServer()
    {
        try
        {
            Debug.Log("Client listening for connect response on " + ConnectPortOnClient);
            IPEndPoint serverIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] receivedBytes = clientUdpClient.Receive(ref serverIpEndPoint);
            string message = Encoding.UTF8.GetString(receivedBytes);
            HandleServerMessage(serverIpEndPoint, message);
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

    private void HandleServerMessage(IPEndPoint serverIpEndPoint, string message)
    {
        Debug.Log($"Received message from server {serverIpEndPoint} ({serverIpEndPoint.Address}): '{message}'");
        ConnectResponseDto connectResponseDto = JsonConverter.FromJson<ConnectResponseDto>(message);
        if (connectResponseDto.clientId.IsNullOrEmpty())
        {
            throw new ConnectRequestException("Malformed ConnectResponse: missing clientId.");
        }

        if (connectResponseDto.microphonePort > 0)
        {
            IPEndPoint serverMicDataEndpoint = new IPEndPoint(serverIpEndPoint.Address, connectResponseDto.microphonePort);
            byte[] dummyMicData = new byte[100];
            ThreadPool.QueueUserWorkItem(poolHandle =>
            {
                Thread.Sleep(100);
                clientUdpClient.Send(dummyMicData, dummyMicData.Length, serverMicDataEndpoint);
                Thread.Sleep(100);
                clientUdpClient.Send(dummyMicData, dummyMicData.Length, serverMicDataEndpoint);
                Thread.Sleep(100);
                clientUdpClient.Send(dummyMicData, dummyMicData.Length, serverMicDataEndpoint);
                Thread.Sleep(100);
                clientUdpClient.Send(dummyMicData, dummyMicData.Length, serverMicDataEndpoint);
            });
        }
    }
    
    private void ClientSendConnectRequest()
    {
        try
        {
            byte[] requestBytes = Encoding.UTF8.GetBytes(new ConnectRequestDto
            {
                clientId = defaultClientId,
                microphoneSampleRate = 22050,
            }.ToJson());
            // UDP Broadcast (255.255.255.255)
            clientUdpClient.Send(requestBytes, requestBytes.Length, "255.255.255.255", ConnectPortOnServer);
            Debug.Log("Client has sent ConnectRequest as broadcast");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void OnDestroy()
    {
        hasBeenDestroyed = true;
        serverUdpClient?.Close();
        clientUdpClient?.Close();
        ClientConnectionManager.RemoveAllLocalClients();
    }
}
