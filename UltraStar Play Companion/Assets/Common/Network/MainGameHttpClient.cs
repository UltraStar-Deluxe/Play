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

    public ReactiveProperty<List<RestApiPermission>> Permissions { get; private set; } = new(new List<RestApiPermission>());

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

    public async Awaitable<string> GetRequestAsync(string path)
    {
        ThrowIfNotConnected();

        string uri = GetUri(path);
        Log.Debug(() => $"Sending GET request to '{uri}'");
        using UnityWebRequest webRequest = UnityWebRequest.Get(uri);
        return await SendWebRequestAsync(webRequest);
    }

    public async Awaitable<string> PostRequestAsync(
        string path,
        string body = "{}",
        string contentType = "application/json")
    {
        ThrowIfNotConnected();

        string uri = GetUri(path);
        Log.Debug(() => $"Sending POST request to '{uri}'");
        using UnityWebRequest webRequest = UnityWebRequest.Post(uri, body, contentType);
        return await SendWebRequestAsync(webRequest);
    }

    public async Awaitable<string> DeleteRequestAsync(string path)
    {
        ThrowIfNotConnected();

        string uri = GetUri(path);
        Log.Debug(() => $"Sending DELETE request to {uri}");
        using UnityWebRequest webRequest = UnityWebRequest.Delete(uri);
        return await SendWebRequestAsync(webRequest);
    }

    private async Awaitable<string> SendWebRequestAsync(UnityWebRequest unityWebRequest)
    {
        AddHeaders(unityWebRequest);
        await WebRequestUtils.SendWebRequestAsync(unityWebRequest);
        return unityWebRequest.downloadHandler?.text;
    }

    private void AddHeaders(UnityWebRequest unityWebRequest)
    {
        unityWebRequest.SetRequestHeader("client-id", settings.ClientId);
        unityWebRequest.SetRequestHeader("client-name", settings.ClientName);
    }

    private void ThrowIfNotConnected()
    {
        if (!IsConnected)
        {
            throw new NotConnectedException("Cannot send request, not connected to main game");
        }
    }
}
