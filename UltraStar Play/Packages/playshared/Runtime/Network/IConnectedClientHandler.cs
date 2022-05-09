using System;

public interface IConnectedClientHandler : IDisposable
{
    string ClientId { get; }
    string ClientName { get; }
    void SendMessageToClient(JsonSerializable jsonSerializable);
    void ReadMessagesFromClient();
    IObservable<JsonSerializable> ReceivedMessageStream { get; }
}
