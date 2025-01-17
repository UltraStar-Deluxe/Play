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

    public async Awaitable<string> GetRequest(string path)
    {
        ThrowIfNotConnected();

        string uri = GetUri(path);
        Log.Debug(() => $"Sending GET request to '{uri}'");
        return await SendRequest(UnityWebRequest.Get(uri));
    }

    public async Awaitable<string> PostRequest(
        string path,
        string body = "{}",
        string contentType = "application/json")
    {
        ThrowIfNotConnected();

        string uri = GetUri(path);
        Log.Debug(() => $"Sending POST request to '{uri}'");
        return await SendRequest(UnityWebRequest.Post(uri, body, contentType));
    }

    public async Awaitable<string> DeleteRequest(string path)
    {
        ThrowIfNotConnected();

        string uri = GetUri(path);
        Log.Debug(() => $"Sending DELETE request to {uri}");
        return await SendRequest(UnityWebRequest.Delete(uri));
    }

    private async Awaitable<string> SendRequest(UnityWebRequest unityWebRequest)
    {
        try
        {
            AddHeaders(unityWebRequest);
            await unityWebRequest.SendWebRequest();

            if (unityWebRequest.result is UnityWebRequest.Result.Success)
            {
                LogRequestSuccess(unityWebRequest);
                return unityWebRequest.downloadHandler?.text;
            }
            else
            {
                string errorMessage = unityWebRequest.error ?? "Unknown error";
                Exception ex = new($"{unityWebRequest.result}: {errorMessage}");
                LogRequestError(unityWebRequest, ex);
                throw new UnityWebRequestException(unityWebRequest);
            }
        }
        catch (Exception ex)
        {
            LogRequestError(unityWebRequest, ex);
            throw ex;
        }
        finally
        {
            unityWebRequest.Dispose();
        }
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
        Log.Verbose(() =>
            $"{unityWebRequest.method} '{unityWebRequest.uri}' has completed. Status: {unityWebRequest.result}, response code: {unityWebRequest.responseCode}, response body: {responseBody}");
    }

    private void ThrowIfNotConnected()
    {
        if (!IsConnected)
        {
            throw new NotConnectedException("Cannot send request, not connected to main game");
        }
    }
}
