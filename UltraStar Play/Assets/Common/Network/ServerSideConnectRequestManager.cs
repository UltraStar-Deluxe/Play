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
    }

    public static ServerSideConnectRequestManager instance;
    public static ServerSideConnectRequestManager Instance
    {
        get
        {
            if (instance == null)
            {
                ServerSideConnectRequestManager instanceInScene = FindObjectOfType<ServerSideConnectRequestManager>();
                if (instanceInScene != null)
                {
                    instanceInScene.InitSingleInstance();
                }
            }
            return instance;
        }
    }

    /**
     * This version number must to be increased when introducing breaking changes.
     */
    public static readonly int protocolVersion = 1;
    
    private UdpClient serverUdpClient;
    private const int ConnectPortOnServer = 34567;
    private const int ConnectPortOnClient = 34568;

    private bool isListeningForConnectRequest;

    private bool hasBeenDestroyed;
    
    private void Start()
    {
        InitSingleInstance();
        if (instance != this)
        {
            return;
        }
        
        serverUdpClient = new UdpClient(ConnectPortOnServer);

        // Move object to top level in scene hierarchy.
        // Otherwise this object will be destroyed with its parent, even when DontDestroyOnLoad is used. 
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            ServerListenForConnectRequest();
        });
    }
    
    private void InitSingleInstance()
    {
        if (!Application.isPlaying)
        {
            return;
        }
            
        if (instance != null)
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
        if (connectRequestDto.protocolVersion != protocolVersion)
        {
            Debug.LogWarning($"Malformed ConnectRequest: protocolVersion does not match (server (main game): {protocolVersion}, client (companion app): {connectRequestDto.protocolVersion}).");
        }
        if (connectRequestDto.clientName.IsNullOrEmpty())
        {
            Debug.LogWarning("Malformed ConnectRequest: missing clientName.");
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
            clientName = connectRequestDto.clientName,
        };
        serverUdpClient.Send(connectResponseDto.ToJson(), clientIpEndPoint);
    }

    private void HandleClientMessageWithMicrophone(IPEndPoint clientIpEndPoint, ConnectRequestDto connectRequestDto)
    {
        ConnectedClientHandler newConnectedClientHandler;
        try
        {
            newConnectedClientHandler = ClientConnectionManager.RegisterClient(clientIpEndPoint, connectRequestDto.clientName, connectRequestDto.microphoneSampleRate);
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
            clientName = connectRequestDto.clientName,
            microphonePort = newConnectedClientHandler.MicrophoneUdpClient.GetPort(),
        };
        serverUdpClient.Send(connectResponseDto.ToJson(), clientIpEndPoint);
    }

    private void OnDestroy()
    {
        hasBeenDestroyed = true;
        serverUdpClient?.Close();
        if (instance == this)
        {
            ClientConnectionManager.RemoveAllLocalClients();
        }
    }
}
