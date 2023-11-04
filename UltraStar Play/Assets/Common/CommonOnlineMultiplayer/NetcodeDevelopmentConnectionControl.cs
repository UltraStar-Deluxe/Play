
using System;
using System.Text;
using CommonOnlineMultiplayer;
using UniInject;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetcodeDevelopmentConnectionControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SteamManager steamManager;

    [Inject]
    private NetworkManager networkManager;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(400, 10, 300, 300));

        if (!networkManager.IsClient
            && !networkManager.IsServer)
        {
            ShowStartButtons();
        }
        else
        {
            ShowStatusLabels();

            ShowMoveButton();
            ShowDisconnectButton();
        }

        GUILayout.EndArea();
    }

    private void ShowStartButtons()
    {
        if (GUILayout.Button("Client"))
        {
            NetworkPlayerConnectionRequestDataDto requestDataDto = new(
                123456789,
                Guid.NewGuid().ToString(),
                "Dummy Client",
                SceneManager.GetActiveScene().name);
            string payload = requestDataDto.ToJson();
            networkManager.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);
            networkManager.StartClient();
        }

        if (GUILayout.Button("Server"))
        {
            networkManager.StartServer();
        }

        // Host is both, client and server
        if (GUILayout.Button("Host"))
        {
            networkManager.StartHost();
        }
    }

    private void ShowStatusLabels()
    {
        var mode = networkManager.IsHost ?
            "Host" : networkManager.IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " +
                        networkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode);
    }

    private void ShowDisconnectButton()
    {
        if (GUILayout.Button("Shutdown"))
        {
            networkManager.Shutdown();
        }
    }

    private void ShowMoveButton()
    {
        if (GUILayout.Button(networkManager.IsServer ? "Move" : "Request Position Change"))
        {
            if (networkManager.IsClient
                && !networkManager.IsServer)
            {
                // On server side, there is no LocalPlayerObject in the SpawnManager.
                NetworkObject localPlayerObject = networkManager.SpawnManager.GetLocalPlayerObject();
                NetworkPlayerControl networkPlayerControl = localPlayerObject.GetComponent<NetworkPlayerControl>();
                networkPlayerControl.Move();
            }
            else if (networkManager.IsServer)
            {
                foreach (NetworkClient networkClient in networkManager.ConnectedClients.Values)
                {
                    NetworkObject remoteNetworkObject = networkClient.PlayerObject;
                    NetworkPlayerControl networkPlayerControl = remoteNetworkObject.GetComponent<NetworkPlayerControl>();
                    networkPlayerControl.Move();
                    networkPlayerControl.CustomParameterClientRpc("demo value", 42);
                }
            }
        }
    }
}
