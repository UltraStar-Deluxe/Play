using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class UdpBroadcaster : MonoBehaviour, INeedInjection
{
    public static UdpBroadcaster Instance { get; private set; }

    private UdpClient udpClientListener;
    private UdpClient udpClientSender;
    private const int ListenPort = 11006;

    private bool hasBeenDestroyed;
    
    private void Start()
    {
        if (Instance != null)
        {
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            Listen();
        });
        Thread.Sleep(100);
        ThreadPool.QueueUserWorkItem(poolHandle =>
        {
            Send();
        });
    }
    
    private void Listen()
    {
        if (udpClientListener != null
            || udpClientSender != null)
        {
            Debug.LogError("Already listening for UDP broadcast");
            return;
        }
        
        try
        {
            udpClientListener = new UdpClient(ListenPort);
            IPEndPoint from = new IPEndPoint(IPAddress.Any, ListenPort);

            while (true)
            {
                Debug.Log("Waiting for broadcast");
                byte[] receive = udpClientListener.Receive(ref from);
                string msg = Encoding.UTF8.GetString(receive);
                Debug.Log($"Received message from {from} ({from.Address}): '{msg}'");
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    private void Send()
    {
        try
        {
            udpClientSender = new UdpClient();
            byte[] sendBuffer = Encoding.UTF8.GetBytes("ThisIsTheMessage");
            udpClientSender.Send(sendBuffer, sendBuffer.Length, "255.255.255.255", ListenPort);
            Debug.Log("Message has been sent");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
