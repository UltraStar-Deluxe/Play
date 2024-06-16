using System;
using UnityEngine.Networking;

public class UnityWebRequestException : Exception
{
    public UnityWebRequest UnityWebRequest { get; private set; }

    public UnityWebRequestException(UnityWebRequest unityWebRequest)
        : base($"UnityWebRequest failed with result: {unityWebRequest.result}. Message: {unityWebRequest.error}")
    {
        UnityWebRequest = unityWebRequest;
    }
}
