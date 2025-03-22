using System;
using System.Linq;
using CircularBuffer;
using LiteNetLib;
using UniRx;
using UnityEngine;

public class CompanionClientHandler : ICompanionClientHandler
{
    private readonly Subject<JsonSerializable> receivedMessageStream = new();
    public IObservable<JsonSerializable> ReceivedMessageStream => receivedMessageStream;

    public NetPeer Peer { get; private set; }
    public string ClientName { get; private set; }
    public string ClientId { get; private set; }

    private readonly CircularBuffer<long> delayValuesInMillis = new(60);
    private readonly CircularBuffer<long> jitterValuesInMillis = new(60);
    private long averageJitterInMillis;
    public long JitterInMillis => averageJitterInMillis;

    private float lastUpdateAverageJitterTimeInSeconds;

    public CompanionClientHandler(
        NetPeer peer,
        string clientName,
        string clientId)
    {
        Peer = peer;
        ClientName = clientName;
        ClientId = clientId;
        if (ClientId.IsNullOrEmpty())
        {
            throw new ArgumentException("Attempt to create CompanionClientHandler without ClientId");
        }
    }

    public void HandleMessageFromClient(string message)
    {
        if (message == null)
        {
            return;
        }

        message = message.Trim();
        if (!message.StartsWith("{")
            || !message.EndsWith("}"))
        {
            Debug.LogWarning($"Received invalid JSON from client: {message}");
            return;
        }

        HandleJsonMessageFromClient(message);
    }

    public void SendMessageToClient(JsonSerializable jsonSerializable)
    {
        Peer.Send(jsonSerializable, DeliveryMethod.ReliableOrdered);
    }

    private void HandleJsonMessageFromClient(string json)
    {
        if (!CompanionAppMessageUtils.TryGetMessageType(json, out CompanionAppMessageType messageType))
        {
            Debug.LogWarning($"Received message with invalid type from client: {json}");
            return;
        }

        switch (messageType)
        {
            case CompanionAppMessageType.BeatPitchEvents:
                BeatPitchEventsDto beatPitchEventsDto = JsonConverter.FromJson<BeatPitchEventsDto>(json);

                UpdateJitterStats(beatPitchEventsDto);

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

        float currentTimeInSeconds = Time.time;
        if (currentTimeInSeconds - lastUpdateAverageJitterTimeInSeconds > 1)
        {
            lastUpdateAverageJitterTimeInSeconds = Time.time;
            averageJitterInMillis = (long)jitterValuesInMillis.Average();
            Log.Verbose(() => $"Average jitter with client {Peer.EndPoint}: {averageJitterInMillis} ms");
        }
    }
}
