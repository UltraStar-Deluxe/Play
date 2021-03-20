using System;
using System.Collections;
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
using AddressFamily = SimpleHttpServerForUnity.AddressFamily;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UdpBroadcaster : MonoBehaviour, INeedInjection
{
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
        
        try
        {
            while (!hasBeenDestroyed)
            {
                ServerAcceptMessageFromClient();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void ServerAcceptMessageFromClient()
    {
        try
        {
            Debug.Log("Server listening for connect request on " + ConnectPortOnServer);
            IPEndPoint clientIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] receive = serverUdpClient.Receive(ref clientIpEndPoint);
            string message = Encoding.UTF8.GetString(receive);
            HandleClientMessage(clientIpEndPoint, message);
        }
        catch (Exception e)
        {
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
        
        ConnectResponseDto connectResponseDto = new ConnectResponseDto
        {
            clientId = connectRequestDto.clientId,
            microphonePort = 0,
        };
        byte[] responseBytes = Encoding.UTF8.GetBytes(connectResponseDto.ToJson());
        serverUdpClient.Send(responseBytes, responseBytes.Length, clientIpEndPoint);
    }
    
    private void ClientListenForConnectResponse()
    {
        if (isListeningForConnectResponse)
        {
            Debug.LogError("Already listening for connect response");
            return;
        }
        isListeningForConnectResponse = true;
        
        try
        {
            while (!hasBeenDestroyed)
            {
                ClientAcceptMessageFromServer();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void ClientAcceptMessageFromServer()
    {
        try
        {
            Debug.Log("Client listening for connect response on " + ConnectPortOnClient);
            IPEndPoint serverIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] receive = clientUdpClient.Receive(ref serverIpEndPoint);
            string message = Encoding.UTF8.GetString(receive);
            HandleServerMessage(serverIpEndPoint, message);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void HandleServerMessage(IPEndPoint serverIpEndPoint, string message)
    {
        Debug.Log($"Received message from server {serverIpEndPoint} ({serverIpEndPoint.Address}): '{message}'");
        ConnectResponseDto connectResponseDto = JsonConverter.FromJson<ConnectResponseDto>(message);
        if (connectResponseDto.clientId.IsNullOrEmpty())
        {
            Debug.LogWarning("Malformed ConnectResponse: missing clientId.");
        }
    }
    
    private void ClientSendConnectRequest()
    {
        try
        {
            byte[] requestBytes = Encoding.UTF8.GetBytes(new ConnectRequestDto
            {
                clientId = defaultClientId,
                requireMicrophonePort = false
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
    }
}
