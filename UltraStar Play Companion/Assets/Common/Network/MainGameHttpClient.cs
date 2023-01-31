using System;
using System.Collections.Generic;
using System.Net;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MainGameHttpClient : MonoBehaviour, INeedInjection
{
    public static MainGameHttpClient Instance => GameObjectUtils.FindComponentWithTag<MainGameHttpClient>("MainGameHttpClient");

    public bool IsConnected => serverIPEndPoint != null && httpServerPort > 0;

    private IPEndPoint serverIPEndPoint;
    private int httpServerPort;

    [Inject]
    private ClientSideConnectRequestManager clientSideConnectRequestManager;

    private void Start()
    {
        clientSideConnectRequestManager.ConnectEventStream
            .Where(connectEvent => connectEvent.IsSuccess)
            .Subscribe(connectEvent =>
            {
                serverIPEndPoint = connectEvent.ServerIpEndPoint;
                httpServerPort = connectEvent.HttpServerPort;
            });
    }

    public string GetUri(string path)
    {
        if (!path.StartsWith("/"))
        {
            path = "/" + path;
        }
        return $"http://{serverIPEndPoint.Address}:{httpServerPort}{path}";
    }

    public UnityWebRequest GetRequest(string path)
    {
        ThrowIfNotConnected();

        string uri = GetUri(path);
        Debug.Log($"Sending GET request to {uri}");
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(uri);
        IObservable<UnityWebRequestAsyncOperation> asyncOperation = unityWebRequest
            .SendWebRequest()
            .AsAsyncOperationObservable();
        HandleRequest(unityWebRequest, asyncOperation);
        return unityWebRequest;
    }

    public UnityWebRequest PostRequest(string path, Dictionary<string, string> formFields = null)
    {
        ThrowIfNotConnected();

        if (formFields == null)
        {
            formFields = new Dictionary<string, string>();
        }

        string uri = GetUri(path);
        Debug.Log($"Sending POST request to {uri}");
        UnityWebRequest unityWebRequest = UnityWebRequest.Post(uri, formFields);
        IObservable<UnityWebRequestAsyncOperation> asyncOperation = unityWebRequest
            .SendWebRequest()
            .AsAsyncOperationObservable();
        HandleRequest(unityWebRequest, asyncOperation);
        return unityWebRequest;
    }

    private void HandleRequest(
        UnityWebRequest unityWebRequest,
        IObservable<UnityWebRequestAsyncOperation> asyncOperationObservable)
    {
        asyncOperationObservable.Subscribe(
            _ => RequestOnNext(unityWebRequest),
            ex => RequestOnError(unityWebRequest, ex),
            () => RequestOnCompleted(unityWebRequest));
    }

    private void RequestOnNext(UnityWebRequest unityWebRequest)
    {
        Debug.Log($"{unityWebRequest.method} '{unityWebRequest.uri}' has updated. Result: {unityWebRequest.result}");
    }

    private void RequestOnError(UnityWebRequest unityWebRequest, Exception ex)
    {
        Debug.LogError($"{unityWebRequest.method} '{unityWebRequest.uri}' failed. Error message: {ex.Message}");
        Debug.LogException(ex);
    }

    private void RequestOnCompleted(UnityWebRequest unityWebRequest)
    {
        Debug.Log($"{unityWebRequest.method} '{unityWebRequest.uri}' has completed. Result: {unityWebRequest.result}");
    }

    private void ThrowIfNotConnected()
    {
        if (!IsConnected)
        {
            throw new NotConnectedException("Cannot send request, not connected to main game");
        }
    }
}
