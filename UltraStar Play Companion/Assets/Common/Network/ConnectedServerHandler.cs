using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ConnectedServerHandler : IConnectedServerHandler, IDisposable
{
    private readonly IClientSideConnectRequestManager clientSideConnectRequestManager;

    private readonly Thread receiveDataThread;
    private readonly Thread serverStillAliveCheckThread;

    private readonly TcpClient tcpClient;
    private readonly NetworkStream tcpClientStream;
    private readonly StreamReader tcpClientStreamReader;
    private readonly StreamWriter tcpClientStreamWriter;
    private readonly object streamReaderLock = new();

    private readonly Subject<JsonSerializable> receivedMessageStream = new();
    public IObservable<JsonSerializable> ReceivedMessageStream => receivedMessageStream;

    private bool isDisposed;

    public ConnectedServerHandler(
        IClientSideConnectRequestManager clientSideConnectRequestManager,
        IPEndPoint serverIpEndPoint)
    {
        this.clientSideConnectRequestManager = clientSideConnectRequestManager;

        tcpClient = new TcpClient();
        tcpClient.NoDelay = true;
        tcpClient.Connect(serverIpEndPoint);
        tcpClientStream = tcpClient.GetStream();
        tcpClientStreamReader = new StreamReader(tcpClientStream);
        tcpClientStreamWriter = new StreamWriter(tcpClientStream);
        tcpClientStreamWriter.AutoFlush = true;

        // Receive messages from server (i.e. from main game)
        receiveDataThread = new Thread(() =>
        {
            while (!isDisposed)
            {
                try
                {
                    ReadMessagesFromServer();
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    clientSideConnectRequestManager.RemoveConnectedServerHandler(this);
                }

                Thread.Sleep(250);
            }
        });
        receiveDataThread.Start();

        serverStillAliveCheckThread = new Thread(() =>
        {
            while (!isDisposed)
            {
                CheckServerStillAlive();
                Thread.Sleep(1500);
            }
        });
        serverStillAliveCheckThread.Start();
    }

    public void ReadMessagesFromServer()
    {
        lock (streamReaderLock)
        {
            while (tcpClientStream != null
                   && tcpClientStream.DataAvailable)
            {
                ReadMessageFromServer();
            }
        }
    }

    private void CheckServerStillAlive()
    {
        try
        {
            // If there is new data available, then the client is still alive.
            if (!tcpClientStream.DataAvailable)
            {
                // Try to send something to the client.
                // If this fails with an Exception, then the connection has been lost and the client has to reconnect.
                SendMessageToServer(new StillAliveCheckDto());
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed sending data to server. Closing connection.");
            clientSideConnectRequestManager.RemoveConnectedServerHandler(this);
        }
    }

    public void SendMessageToServer(JsonSerializable jsonSerializable)
    {
        try
        {
            tcpClientStreamWriter.WriteLine(jsonSerializable.ToJson());
            tcpClientStreamWriter.Flush();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"Failed to send pitch to server");
            clientSideConnectRequestManager.RemoveConnectedServerHandler(this);
        }
    }

    private void ReadMessageFromServer()
    {
        string receivedLine = tcpClientStreamReader.ReadLine();
        if (receivedLine.IsNullOrEmpty())
        {
            return;
        }

        receivedLine = receivedLine.Trim();
        if (!receivedLine.StartsWith("{")
            || !receivedLine.EndsWith("}"))
        {
            Debug.LogWarning($"Received invalid message from server: {receivedLine}");
            return;
        }

        HandleJsonMessageFromServer(receivedLine);
    }

    private void HandleJsonMessageFromServer(string json)
    {
        if (!CompanionAppMessageUtils.TryGetMessageType(json, out CompanionAppMessageType messageType))
        {
            return;
        }

        switch (messageType)
        {
            case CompanionAppMessageType.StillAliveCheck:
                // Nothing to do. If the connection would not be still alive anymore, then this message would have failed already.
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
            default:
                Debug.Log($"Unknown MessageType {messageType} in JSON from server: {json}");
                return;
        }
    }

    public void Dispose()
    {
        isDisposed = true;
        tcpClientStream?.Close();
        tcpClient?.Close();
    }
}
