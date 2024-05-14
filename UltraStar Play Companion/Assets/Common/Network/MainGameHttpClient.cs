using System;
using System.Collections.Generic;
using System.Net;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.Networking;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MainGameHttpClient : AbstractSingletonBehaviour, INeedInjection
{
    public static MainGameHttpClient Instance => GameObjectUtils.FindComponentWithTag<MainGameHttpClient>("MainGameHttpClient");

    public bool IsConnected => serverIPEndPoint != null && httpServerPort > 0;

    private IPEndPoint serverIPEndPoint;
    private int httpServerPort;

    [Inject]
    private ClientSideCompanionClientManager clientSideCompanionClientManager;

    [Inject]
    private Settings settings;

    private readonly Subject<bool> connectionEventStream = new();
    public IObservable<bool> ConnectionEventStream => connectionEventStream;

    public ReactiveProperty<List<HttpApiPermission>> Permissions { get; private set; } = new(new List<HttpApiPermission>());

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        clientSideCompanionClientManager.ConnectEventStream
            .Where(connectEvent => connectEvent.IsSuccess)
            .Subscribe(connectEvent =>
            {
                serverIPEndPoint = connectEvent.ServerIpEndPoint;
                httpServerPort = connectEvent.HttpServerPort;
                Permissions.Value = connectEvent.Permissions ?? new();
                connectionEventStream.OnNext(true);
            });

        clientSideCompanionClientManager.ReceivedMessageStream
            .Subscribe(dto =>
            {
                if (dto is PermissionsMessageDto permissionsMessageDto)
                {
                    Permissions.Value = permissionsMessageDto.Permissions;
                }
            });

        Permissions.Subscribe(newPermissions =>
            Debug.Log($"Permissions changed: {newPermissions.JoinWith(", ")}"));
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
        Log.Debug(() => $"Sending GET request to {uri}");
        UnityWebRequest unityWebRequest = UnityWebRequest.Get(uri);
        SendRequest(unityWebRequest, onSuccess, onError);
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
        Log.Debug(() => $"Sending POST request to '{uri}'");
        UnityWebRequest unityWebRequest = UnityWebRequest.Post(uri, body, contentType);
        SendRequest(unityWebRequest, onSuccess, onError);
    }

    public void DeleteRequest(
        string path,
        Action<string> onSuccess = null,
        Action<Exception> onError = null)
    {
        ThrowIfNotConnected();

        string uri = GetUri(path);
        Log.Debug(() => $"Sending DELETE request to {uri}");
        UnityWebRequest unityWebRequest = UnityWebRequest.Delete(uri);
        SendRequest(unityWebRequest, onSuccess, onError);
    }

    private void SendRequest(
        UnityWebRequest unityWebRequest,
        Action<string> onSuccess,
        Action<Exception> onError)
    {
        AddHeaders(unityWebRequest);
        unityWebRequest.SendWebRequest();

        void WrappedOnSuccess(DownloadHandler downloadHandler)
        {
            string response = downloadHandler?.text;
            LogRequestSuccess(unityWebRequest);
            onSuccess?.Invoke(response);
        }

        void WrappedOnError(Exception ex)
        {
            LogRequestError(unityWebRequest, ex);
            onError?.Invoke(ex);
        }

        StartCoroutine(CoroutineUtils.WebRequestCoroutine(unityWebRequest,
            WrappedOnSuccess,
            ex => WrappedOnError(ex)));
    }

    private void AddHeaders(UnityWebRequest unityWebRequest)
    {
        unityWebRequest.SetRequestHeader("client-id", settings.ClientId);
        unityWebRequest.SetRequestHeader("client-name", settings.ClientName);
    }

    private void LogRequestError(UnityWebRequest unityWebRequest, Exception ex)
    {
        string responseBody = unityWebRequest.downloadHandler?.text;
        Debug.LogError($"{unityWebRequest.method} '{unityWebRequest.uri}' has failed. Status: {unityWebRequest.result}, response code: {unityWebRequest.responseCode}, error message: {ex.Message}, response body: {responseBody}");
        Debug.LogException(ex);
    }

    private void LogRequestSuccess(UnityWebRequest unityWebRequest)
    {
        string responseBody = unityWebRequest.downloadHandler?.text;
        Log.Debug(() => $"{unityWebRequest.method} '{unityWebRequest.uri}' has completed. Status: {unityWebRequest.result}, response code: {unityWebRequest.responseCode}, response body: {responseBody}");
    }

    private void ThrowIfNotConnected()
    {
        if (!IsConnected)
        {
            throw new NotConnectedException("Cannot send request, not connected to main game");
        }
    }
}
