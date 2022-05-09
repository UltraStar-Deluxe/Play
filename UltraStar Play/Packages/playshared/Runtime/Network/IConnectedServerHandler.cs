using System;

public interface IConnectedServerHandler
{
    void SendMessageToServer(JsonSerializable jsonSerializable);
    void ReadMessagesFromServer();
    IObservable<JsonSerializable> ReceivedMessageStream { get; }
}
