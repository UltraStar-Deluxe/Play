using System;

public interface IConnectedClientHandler
{
    string ClientId { get; }
    string ClientName { get; }
    long JitterInMillis { get; }
    void SendMessageToClient(JsonSerializable jsonSerializable);
    void HandleMessageFromClient(string message);
    IObservable<JsonSerializable> ReceivedMessageStream { get; }
}
