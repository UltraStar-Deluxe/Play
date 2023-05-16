using System;

public interface IConnectedClientHandler : IDisposable
{
    string ClientId { get; }
    string ClientName { get; }
    long JitterInMillis { get; }
    void SendMessageToClient(JsonSerializable jsonSerializable);
    void ReadMessagesFromClient();
    IObservable<JsonSerializable> ReceivedMessageStream { get; }
}
