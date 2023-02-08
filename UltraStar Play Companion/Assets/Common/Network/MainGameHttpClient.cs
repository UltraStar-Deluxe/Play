using System;
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

    [Inject]
    private UnityWebRequestManager webRequestManager;
    
    private readonly Subject<bool> connectionEventStream = new();
    public IObservable<bool> ConnectionEventStream => connectionEventStream;

    private void Start()
    {
        clientSideConnectRequestManager.ConnectEventStream
            .Where(connectEvent => connectEvent.IsSuccess)
            .Subscribe(connectEvent =>
            {
                serverIPEndPoint = connectEvent.ServerIpEndPoint;
                httpServerPort = connectEvent.HttpServerPort;

                connectionEventStream.OnNext(true);
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

    public void GetRequest(
        string path,
        Action<string> onSuccess = null,
        Action<Exception> onError = null)
    {
        ThrowIfNotConnected();

        string uri = GetUri(path);
        Debug.Log($"Sending GET request to {uri}");
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(uri);
        unityWebRequest.SendWebRequest();
        HandleRequest(unityWebRequest, onSuccess, onError);
    }

    public void PostRequest(
        string path,
        string body = "{}",
        string contentType = "application/json",
        Action<string> onSuccess = null,
        Action<Exception> onError = null)
    {
        ThrowIfNotConnected();

        string uri = GetUri(path);
        Debug.Log($"Sending POST request to {uri}");
        UnityWebRequest unityWebRequest = UnityWebRequest.Post(uri, body, contentType);
        unityWebRequest.SendWebRequest();
        HandleRequest(unityWebRequest, onSuccess, onError);
    }

    public void DeleteRequest(
        string path,
        Action<string> onSuccess = null,
        Action<Exception> onError = null)
    {
        ThrowIfNotConnected();

        string uri = GetUri(path);
        Debug.Log($"Sending DELETE request to {uri}");
        UnityWebRequest unityWebRequest = UnityWebRequest.Delete(uri);
        unityWebRequest.SendWebRequest();
        HandleRequest(unityWebRequest, onSuccess, onError);
    }
    
    private void HandleRequest(
        UnityWebRequest unityWebRequest,
        Action<string> onSuccess,
        Action<Exception> onError)
    {
        void WrappedOnError(Exception ex)
        {
            RequestOnError(unityWebRequest, ex);
            onError?.Invoke(ex);
        }

        webRequestManager.AddUnityWebRequest(unityWebRequest,
            onSuccess,
            ex => WrappedOnError(ex));

        // TODO: onComplete is called prematurely when using Observable (see https://github.com/neuecc/UniRx/issues/530)
        // asyncOperationObservable.Subscribe(
        //     _ => RequestOnNext(unityWebRequest),
        //     ex => RequestOnError(unityWebRequest, ex),
        //     () => RequestOnCompleted(unityWebRequest, onSuccess));
    }

    private void RequestOnNext(UnityWebRequest unityWebRequest)
    {
        Debug.Log($"{unityWebRequest.method} '{unityWebRequest.uri}' has updated. Status: {unityWebRequest.result}, response code: {unityWebRequest.responseCode}");
    }

    private void RequestOnError(UnityWebRequest unityWebRequest, Exception ex)
    {
        string responseBody = unityWebRequest.downloadHandler.text;
        Debug.LogError($"{unityWebRequest.method} '{unityWebRequest.uri}' has failed. Status: {unityWebRequest.result}, response code: {unityWebRequest.responseCode}, error message: {ex.Message}, response body: {responseBody}");
        Debug.LogException(ex);
    }

    private void RequestOnCompleted(UnityWebRequest unityWebRequest, Action<string> onSuccess)
    {
        string responseBody = unityWebRequest.downloadHandler.text;
        Debug.Log($"{unityWebRequest.method} '{unityWebRequest.uri}' has completed. Status: {unityWebRequest.result}, response code: {unityWebRequest.responseCode}, response body: {responseBody}");
        if (onSuccess != null)
        {
            MainThreadDispatcher.Send(_ => onSuccess(responseBody), null);
        }
    }

    private void ThrowIfNotConnected()
    {
        if (!IsConnected)
        {
            throw new NotConnectedException("Cannot send request, not connected to main game");
        }
    }
}
