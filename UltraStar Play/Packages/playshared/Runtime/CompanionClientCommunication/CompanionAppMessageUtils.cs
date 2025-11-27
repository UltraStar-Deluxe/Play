using System;
using UnityEngine;

public static class CompanionAppMessageUtils
{
    public static bool TryGetMessageType(string json, out CompanionAppMessageType messageType)
    {
        try
        {
            // Use UnityEngine.JsonUtility for better performance. There can be hundreds of messages per second such that Newtonsoft.Json is too slow in this case.
            HasMessageType hasMessageType = JsonUtility.FromJson<HasMessageType>(json);
            if (Enum.TryParse(hasMessageType.MessageType, out messageType))
            {
                // OK. MessageType is valid.
                return true;
            }
            else
            {
                throw new CompanionClientException("Could not determine message type.");
            }
        }
        catch (Exception e)
        {
            Debug.Log($"Exception while parsing message from server. Exception message: {e.Message}. JSON: {json}");
            Debug.LogException(e);
            messageType = CompanionAppMessageType.Unknown;
            return false;
        }
    }

    [Serializable] // Serializable is required for UnityEngine.JsonUtility
    public class HasMessageType
    {
        public string MessageType { get; set; }
    }
}
