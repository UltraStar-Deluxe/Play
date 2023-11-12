using System;
using System.Linq;
using System.Text;
using CommonOnlineMultiplayer;
using SteamOnlineMultiplayer;
using UniInject;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetcodeDevUtilityControl : MonoBehaviour, INeedInjection
{
    [Inject]
    private SteamManager steamManager;

    [Inject]
    private SteamLobbyMemberManager steamLobbyMemberManager;

    [Inject]
    private NetworkManager networkManager;

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(100, 10, 300, 300));

        if (!networkManager.IsClient
            && !networkManager.IsServer)
        {
        }
        else
        {
            ShowStatusLabels();
            ShowMoveButton();
        }

        GUILayout.EndArea();
    }

    private void ShowStatusLabels()
    {
        string mode = GetNetworkManagerMode(networkManager);

        GUILayout.Label("Transport: " +
                        networkManager.NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode.ToUpperInvariantFirstChar());

        if (networkManager.IsServer)
        {
            GUILayout.Label("Connected Netcode Client IDs: " + networkManager
                .ConnectedClients
                .Values
                .Select(netcodeClient => netcodeClient.ClientId)
                .JoinWith(", ", "", ""));
        }

        GUILayout.Label("Connected Steam Lobby Members: " + steamLobbyMemberManager.GetSteamLobbyMembers()
            .Select(memberData => memberData.ToString())
            .JoinWith(", ", "", ""));
    }

    private static string GetNetworkManagerMode(NetworkManager networkManager)
    {
        if (networkManager.IsHost)
        {
            return "host";
        }
        else if (networkManager.IsServer)
        {
            return "server";
        }
        else if (networkManager.IsClient)
        {
            return "client";
        }
        return "none";
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
                if (localPlayerObject == null)
                {
                    throw new Exception("Missing local PlayerObject");
                }

                LobbyMemberNetworkBehaviour lobbyMemberNetworkBehaviour = localPlayerObject.GetComponent<LobbyMemberNetworkBehaviour>();
                if (lobbyMemberNetworkBehaviour == null)
                {
                    throw new Exception("Missing NetworkPlayerControl");
                }

                lobbyMemberNetworkBehaviour.Move();
            }
            else if (networkManager.IsServer)
            {
                foreach (NetworkClient networkClient in networkManager.ConnectedClients.Values)
                {
                    NetworkObject remoteNetworkObject = networkClient.PlayerObject;
                    if (remoteNetworkObject == null)
                    {
                        throw new Exception("Missing remote PlayerObject");
                    }

                    LobbyMemberNetworkBehaviour lobbyMemberNetworkBehaviour = remoteNetworkObject.GetComponent<LobbyMemberNetworkBehaviour>();
                    if (lobbyMemberNetworkBehaviour == null)
                    {
                        throw new Exception("Missing NetworkPlayerControl");
                    }

                    lobbyMemberNetworkBehaviour.Move();
                    lobbyMemberNetworkBehaviour.CustomParameterClientRpc("demo value", 42);
                }
            }
        }
    }
}
