using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

public static class LiteNetLibExtensions
{
    public static void Send(this NetPeer peer, JsonSerializable jsonSerializable, DeliveryMethod deliveryMethod)
    {
        if (jsonSerializable == null)
        {
            return;
        }
        
        peer.Send(jsonSerializable.ToJson(), deliveryMethod);
    }
    
    public static void Send(this NetPeer peer, string message, DeliveryMethod deliveryMethod)
    {
        if (message == null)
        {
            return;
        }
        
        NetDataWriter resp = new();
        resp.Put(message);
        peer.Send(resp, deliveryMethod);
    }
    
    public static LogType ToUnityLogType(this NetLogLevel netLogLevel)
    {
        switch(netLogLevel)
        {
            case NetLogLevel.Trace:
                return LogType.Log;
            case NetLogLevel.Info:
                return LogType.Log;
            case NetLogLevel.Warning:
                return LogType.Warning;
            case NetLogLevel.Error:
                return LogType.Error;
            default:
                return LogType.Log;
        }
    }
}
