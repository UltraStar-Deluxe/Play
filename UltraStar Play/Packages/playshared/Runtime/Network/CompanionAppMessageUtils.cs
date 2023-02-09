using System;
using UnityEngine;

public static class CompanionAppMessageUtils
{
    public static bool TryGetMessageType(string json, out CompanionAppMessageType messageType)
    {
        try
        {
            HasMessageType hasMessageType = JsonConverter.FromJson<HasMessageType>(json, false);
            if (Enum.TryParse(hasMessageType.MessageType, out messageType))
            {
                // OK. MessageType is valid.
                return true;
            }
            else
            {
                throw new Exception("Could not determine message type.");
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
    
    private class HasMessageType
    {
        public string MessageType { get; set; }
    }
}
