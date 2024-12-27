using System;
using UnityEngine.Networking;

public class UnityWebRequestException : Exception
{
    public UnityWebRequest UnityWebRequest { get; private set; }

    public UnityWebRequestException(UnityWebRequest unityWebRequest)
        : base($"UnityWebRequest failed: method '{unityWebRequest.method}', url '{unityWebRequest.url}', result '{unityWebRequest.result}', error '{unityWebRequest.error}'")
    {
        UnityWebRequest = unityWebRequest;
    }
}
