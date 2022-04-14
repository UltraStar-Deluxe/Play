using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UniRx;
using UnityEngine;

public class ConnectedClientHandler : IConnectedClientHandler
{
    private Subject<BeatPitchEvent> pitchEventStream = new();
    public IObservable<BeatPitchEvent> PitchEventStream => pitchEventStream;

    public IPEndPoint ClientIpEndPoint { get; private set; }
    public string ClientName { get; private set; }
    public string ClientId { get; private set; }
    public TcpListener ClientTcpListener { get; private set; }
    public int SampleRateHz => 44100;

    private bool isDisposed;

    private TcpClient tcpClient;
    private NetworkStream tcpClientStream;
    private StreamReader tcpClientStreamReader;
    private StreamWriter tcpClientStreamWriter;

    private readonly ServerSideConnectRequestManager serverSideConnectRequestManager;

    private readonly Thread receiveDataThread;
    private readonly Thread clientStillAliveCheckThread;
    
    public ConnectedClientHandler(
        ServerSideConnectRequestManager serverSideConnectRequestManager,
        IPEndPoint clientIpEndPoint,
        string clientName,
        string clientId)
    {
        this.serverSideConnectRequestManager = serverSideConnectRequestManager;
        ClientIpEndPoint = clientIpEndPoint;
        ClientName = clientName;
        ClientId = clientId;
        if (ClientId.IsNullOrEmpty())
        {
            throw new ArgumentException("Attempt to create ConnectedClientHandler without ClientId");
        }

        ClientTcpListener = new TcpListener(IPAddress.Any, 0);
        ClientTcpListener.Start();
        
        Debug.Log($"Started TcpListener on port {ClientTcpListener.GetPort()} to receive messages from Companion App");
        receiveDataThread = new Thread(() =>
        {
            while (!isDisposed)
            {
                int sleepTimeInMillis = 250;
                if (tcpClient == null)
                {
                    try
                    {
                        tcpClient = ClientTcpListener.AcceptTcpClient();
                        tcpClientStream = tcpClient.GetStream();
                        tcpClientStreamReader = new StreamReader(tcpClientStream);
                        tcpClientStreamWriter = new StreamWriter(tcpClientStream);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error when accepting TcpClient for Companion App. Closing TcpListener.");
                        Debug.LogException(e);
                        this.serverSideConnectRequestManager.RemoveConnectedClientHandler(this);
                        return;
                    }
                }
                else
                {
                    try
                    {
                        while (tcpClientStream.DataAvailable)
                        {
                            ReadMessageFromClient();
                            // Check for next message soon (33 milliseconds is approx. 30 FPS).
                            sleepTimeInMillis = 33;
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error when reading TcpClient message. Closing TcpListener.");
                        Debug.LogException(e);
                        this.serverSideConnectRequestManager.RemoveConnectedClientHandler(this);
                        return;
                    }
                }

                Thread.Sleep(sleepTimeInMillis);
            }
        });
        receiveDataThread.Start();
        
        clientStillAliveCheckThread = new Thread(() =>
        {
            while (!isDisposed)
            {
                if (tcpClient != null
                    && tcpClientStream != null)
                {
                    CheckClientStillAlive();
                }
                Thread.Sleep(1500);
            }
        });
        clientStillAliveCheckThread.Start();
    }
    
    private void CheckClientStillAlive()
    {
        try
        {
            // If there is new data available, then the client is still alive.
            if (!tcpClientStream.DataAvailable)
            {
                // Try to send something to the client.
                // If this fails with an Exception, then the connection has been lost and the client has to reconnect.
                tcpClientStreamWriter.WriteLine(new StillAliveCheckDto().ToJson());
                tcpClientStreamWriter.Flush();
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError("Failed sending data to client. Removing ConnectedClientHandler.");
            serverSideConnectRequestManager.RemoveConnectedClientHandler(this);
        }
    }

    private void ReadMessageFromClient()
    {
        string line = tcpClientStreamReader.ReadLine();
        if (line.IsNullOrEmpty())
        {
            return;
        }

        line = line.Trim();
        if (!line.StartsWith("{")
            || !line.EndsWith("}"))
        {
            Debug.LogWarning("Received invalid JSON from client.");
            return;
        }

        HandleJsonMessageFromClient(line);
    }

    public void SendMessageToClient(JsonSerializable jsonSerializable)
    {
        if (tcpClient != null
            && tcpClientStream != null
            && tcpClientStreamWriter != null
            && tcpClientStream.CanWrite)
        {
            tcpClientStreamWriter.WriteLine(jsonSerializable.ToJson());
            tcpClientStreamWriter.Flush();
        }
        else
        {
            Debug.LogWarning("Cannot send message to client.");
        }
    }

    private void HandleJsonMessageFromClient(string json)
    {
        CompanionAppMessageDto companionAppMessageDto = JsonConverter.FromJson<CompanionAppMessageDto>(json);
        switch (companionAppMessageDto.MessageType)
        {
            case CompanionAppMessageType.StillAliveCheck:
                // Nothing to do. If the connection would not be still alive anymore, then this message would have failed already.
                return;
            case CompanionAppMessageType.BeatPitchEvent:
                FireBeatPitchEventFromCompanionApp(JsonConverter.FromJson<BeatPitchEventDto>(json));
                return;
            default:
                Debug.Log($"Unknown MessageType {companionAppMessageDto.MessageType} in JSON from server: {json}");
                return;
        }
    }

    private void FireBeatPitchEventFromCompanionApp(BeatPitchEventDto beatPitchEventDto)
    {
        pitchEventStream.OnNext(new BeatPitchEvent(beatPitchEventDto.MidiNote, beatPitchEventDto.Beat));
    }

    public void Dispose()
    {
        isDisposed = true;
        tcpClientStream?.Close();
        tcpClient?.Close();
        ClientTcpListener?.Stop();
    }
}
