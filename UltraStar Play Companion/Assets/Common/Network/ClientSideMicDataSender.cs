using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UniInject;
using UnityEngine;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ClientSideMicDataSender : MonoBehaviour, INeedInjection
{
    public static ClientSideMicDataSender Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<ClientSideMicDataSender>("ClientSideMicrophoneDataSender");
        }
    }
    
    [Inject]
    private ClientSideConnectRequestManager clientSideConnectRequestManager;

    [Inject]
    private ClientSideMicSampleRecorder clientSideMicSampleRecorder;

    [Inject]
    private Settings settings;
    
    private TcpClient clientMicDataSender;
    private NetworkStream clientMicDataSenderNetworkStream;
    public IPEndPoint serverMicDataReceiverEndPoint;

    private byte[] receiveByteArray;

    private Thread receiveDataThread;
    
    private void Start()
    {
        clientSideConnectRequestManager.ConnectEventStream.Subscribe(UpdateConnectionStatus);
        clientSideMicSampleRecorder.RecordingEventStream.Subscribe(HandleNewMicSamples);

        // Receive data from server.
        // So far, the server only sends a still-alive check, which fails automatically when the connection is lost.
        receiveByteArray = new byte[2048];
        receiveDataThread = new Thread(() => 
        {
            while (true)
            {
                if (serverMicDataReceiverEndPoint != null
                    && clientMicDataSender != null
                    && clientMicDataSenderNetworkStream != null)
                {
                    ReceiveServerData();
                }
                Thread.Sleep(250);
            }
        });
        receiveDataThread.Start();
    }

    private void HandleNewMicSamples(RecordingEvent recordingEvent)
    {
        if (serverMicDataReceiverEndPoint != null
            && clientMicDataSender != null
            && clientMicDataSenderNetworkStream != null)
        {
            SendMicData(recordingEvent);
        }
    }

    private void ReceiveServerData()
    {
        int receivedByteCount;
        while (clientMicDataSenderNetworkStream.DataAvailable
               && (receivedByteCount = clientMicDataSenderNetworkStream.Read(receiveByteArray, 0, receiveByteArray.Length)) > 0)
        {
            // Do nothing.
            // Debug.Log($"Received {receivedByteCount} bytes from main game (still-alive check).");
        }
    }

    private void SendMicData(RecordingEvent recordingEvent)
    {
        if (recordingEvent.NewSampleCount >= clientSideMicSampleRecorder.SampleRate.Value - 1)
        {
            Debug.LogError("Attempt to send complete mic buffer at once");
            return;
        }
        // Copy from float array to byte array. Note that in a float there are sizeof(float) bytes.
        byte[] newByteData = new byte[recordingEvent.NewSampleCount * sizeof(float)];
        Buffer.BlockCopy(
            recordingEvent.MicSamples, recordingEvent.NewSamplesStartIndex * sizeof(float),
            newByteData, 0,
            recordingEvent.NewSampleCount * sizeof(float));

        try
        {
            // DateTime now = DateTime.Now;
            // Debug.Log($"Send data: {newByteData.Length} bytes ({recordingEvent.NewSampleCount} samples) at {now}:{now.Millisecond}");
            clientMicDataSenderNetworkStream.Write(newByteData, 0, newByteData.Length);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed sending mic data: {newByteData.Length} bytes ({recordingEvent.NewSampleCount} samples)");
            clientSideConnectRequestManager.CloseConnectionAndReconnect();
        }
    }

    private void UpdateConnectionStatus(ConnectEvent connectEvent)
    {
        if (connectEvent.IsSuccess
            && connectEvent.MicrophonePort > 0
            && connectEvent.ServerIpEndPoint != null)
        {
            serverMicDataReceiverEndPoint = new IPEndPoint(connectEvent.ServerIpEndPoint.Address, connectEvent.MicrophonePort);
            if (connectEvent.MicrophoneSampleRate > 0
                && connectEvent.MicrophoneSampleRate != settings.SampleRate)
            {
                Debug.Log($"Received new sample rate: {settings.SampleRate}");
                settings.SampleRate = connectEvent.MicrophoneSampleRate;
                if (clientSideMicSampleRecorder.SampleRate.Value != settings.SampleRate)
                {
                    clientSideMicSampleRecorder.SetSampleRate(settings.SampleRate);
                    // Try again with new SampleRate received from main game.
                    CloseNetworkConnection();
                    return;
                }
            }

            CloseNetworkConnection();
            try
            {
                clientMicDataSender = new TcpClient();
                clientMicDataSender.Connect(serverMicDataReceiverEndPoint);
                clientMicDataSenderNetworkStream = clientMicDataSender.GetStream();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                CloseNetworkConnection();                
            }
        }
        else
        {
            CloseNetworkConnection();
            serverMicDataReceiverEndPoint = null;
            clientSideMicSampleRecorder.StopRecording();
        }
    }

    private void OnDestroy()
    {
        CloseNetworkConnection();
    }

    private void CloseNetworkConnection()
    {
        clientMicDataSenderNetworkStream?.Close();
        clientMicDataSenderNetworkStream = null;
        clientMicDataSender?.Close();
        clientMicDataSender = null;
    }
}
