using System.Net;
using UniInject;
using UniRx;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class AbstractHttpRequestor : MonoBehaviour, INeedInjection
{
    protected IPEndPoint serverIPEndPoint;
    protected int httpServerPort;

    [Inject]
    protected ClientSideConnectRequestManager clientSideConnectRequestManager;
    
    protected void Start()
    {
        clientSideConnectRequestManager.ConnectEventStream
            .Where(connectEvent => connectEvent.IsSuccess)
            .Subscribe(connectEvent =>
            {
                serverIPEndPoint = connectEvent.ServerIpEndPoint;
                httpServerPort = connectEvent.HttpServerPort;
            });
    }
}
