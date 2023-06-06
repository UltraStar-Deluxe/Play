using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using CircularBuffer;
using UniRx;
using UnityEngine;

public class ConnectedClientHandler : IConnectedClientHandler
{
    private const int MaxSendMessageAttemptCount = 5;
    
    private readonly Subject<JsonSerializable> receivedMessageStream = new();
    public IObservable<JsonSerializable> ReceivedMessageStream => receivedMessageStream;

    public IPEndPoint ClientIpEndPoint { get; private set; }
    public string ClientName { get; private set; }
    public string ClientId { get; private set; }
    public TcpListener ClientTcpListener { get; private set; }

    private readonly IServerSideConnectRequestManager serverSideConnectRequestManager;

    private readonly object streamReaderLock = new();
    private readonly Thread receiveDataThread;
    private readonly Thread clientStillAliveCheckThread;

    private TcpClient tcpClient;
    private NetworkStream tcpClientStream;
    private StreamReader tcpClientStreamReader;
    private StreamWriter tcpClientStreamWriter;

    private bool isDisposed;

    private readonly CircularBuffer<long> delayValuesInMillis = new(10);
    private readonly CircularBuffer<long> jitterValuesInMillis = new(10);
    private long averageJitterInMillis;
    public long JitterInMillis => averageJitterInMillis;

    public ConnectedClientHandler(
        IServerSideConnectRequestManager serverSideConnectRequestManager,
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
                if (tcpClient == null)
                {
                    try
                    {
                        tcpClient = ClientTcpListener.AcceptTcpClient();
                        tcpClient.NoDelay = true;
                        tcpClientStream = tcpClient.GetStream();
                        tcpClientStreamReader = new StreamReader(tcpClientStream);
                        tcpClientStreamWriter = new StreamWriter(tcpClientStream);
                        tcpClientStreamWriter.AutoFlush = true;
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
                        ReadMessagesFromClient();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("Error when reading TcpClient message. Closing TcpListener.");
                        Debug.LogException(e);
                        this.serverSideConnectRequestManager.RemoveConnectedClientHandler(this);
                        return;
                    }
                }

                Thread.Sleep(250);
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

    public void ReadMessagesFromClient()
    {
        lock (streamReaderLock)
        {
            while (tcpClientStream != null
                   && tcpClientStream.DataAvailable)
            {
                ReadMessageFromClient();
            }
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
        DoSendMessageToClient(jsonSerializable, 1);
    }
    
    private void DoSendMessageToClient(JsonSerializable jsonSerializable, int attempt)
    {
        if (tcpClient != null
            && tcpClientStream != null
            && tcpClientStreamWriter != null
            && tcpClientStream.CanWrite)
        {
            tcpClientStreamWriter.WriteLine(jsonSerializable.ToJson());
            tcpClientStreamWriter.Flush();
        }
        else if (attempt < MaxSendMessageAttemptCount)
        {
            Debug.LogWarning($"Cannot send message to client. Trying to send again after delay (attempt: {attempt}, tcpClient == null: {tcpClient == null}, tcpClientStream == null: {tcpClientStream == null}, tcpClientStreamWriter == null: {tcpClientStreamWriter == null}, tcpClientStream.CanWrite: {tcpClientStream?.CanWrite})");
            TrySendMessageToClientAfterDelay(jsonSerializable, attempt + 1, 0.1f);
        }
        else
        {
            Debug.LogWarning($"Cannot send message to client. Failed for good, not trying to send again later. (tcpClient == null: {tcpClient == null}, tcpClientStream == null: {tcpClientStream == null}, tcpClientStreamWriter == null: {tcpClientStreamWriter == null}, tcpClientStream.CanWrite: {tcpClientStream?.CanWrite})");
        }
    }

    private void TrySendMessageToClientAfterDelay(JsonSerializable jsonSerializable, int attempt, float delayInSeconds)
    {
        MainThreadDispatcher.StartCoroutine(
            CoroutineUtils.ExecuteAfterDelayInSeconds(delayInSeconds, () => DoSendMessageToClient(jsonSerializable, attempt)));
    }

    private void HandleJsonMessageFromClient(string json)
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
            case CompanionAppMessageType.BeatPitchEvents:
                // Debug.Log("ReceivedMessageFromClient - BeatPitchEventsDto: " + json);
                BeatPitchEventsDto beatPitchEventsDto = JsonConverter.FromJson<BeatPitchEventsDto>(json);
                
                UpdateJitterStats(beatPitchEventsDto);
                // Debug.Log($"Average jitter: {averageJitterInMillis} ms");
                
                receivedMessageStream.OnNext(beatPitchEventsDto);
                return;
            default:
                Debug.Log($"Unknown MessageType {messageType} in JSON from server: {json}");
                return;
        }
    }

    private void UpdateJitterStats(CompanionAppMessageDto companionAppMessageDto)
    {
        long messageDtoUnixTimeInMillis = companionAppMessageDto.UnixTimeMilliseconds;
        
        long lastMessageDelay = !delayValuesInMillis.IsEmpty
            ? delayValuesInMillis.LastOrDefault()
            : 0;
        long currentMessageDelayInMillis = TimeUtils.GetUnixTimeMilliseconds() - messageDtoUnixTimeInMillis;
        delayValuesInMillis.PushBack(currentMessageDelayInMillis);
        long currentMessageJitterInMillis = delayValuesInMillis.Count >= 2
            ? Math.Abs(currentMessageDelayInMillis - lastMessageDelay)
            : 0;

        jitterValuesInMillis.PushBack(currentMessageJitterInMillis);
        averageJitterInMillis = (long)jitterValuesInMillis.Average();
    }

    public void Dispose()
    {
        isDisposed = true;
        tcpClientStream?.Close();
        tcpClient?.Close();
        ClientTcpListener?.Stop();
    }
}
