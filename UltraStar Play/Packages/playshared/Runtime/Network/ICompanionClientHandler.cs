using System;

public interface ICompanionClientHandler
{
    string ClientId { get; }
    string ClientName { get; }
    long JitterInMillis { get; }
    void SendMessageToClient(JsonSerializable jsonSerializable);
    void HandleMessageFromClient(string message);
    IObservable<JsonSerializable> ReceivedMessageStream { get; }
}
