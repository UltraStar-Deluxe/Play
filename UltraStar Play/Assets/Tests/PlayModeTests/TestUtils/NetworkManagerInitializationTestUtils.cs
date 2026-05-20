using CommonOnlineMultiplayer;
using Unity.Netcode;
using UnityEngine;
using UnityEditor;

public static class NetworkManagerInitializationTestUtils
{
    private const string NetworkManagerPrefabPath = "Assets/Common/OnlineMultiplayer/NetworkManager.prefab";

    public static void InitNetworkManagerSingleton()
    {
        if (NetworkManager.Singleton != null)
        {
            return;
        }

        Debug.Log("Initializing NetworkManager.Singleton");
        try
        {
            NetworkManagerInitialization.InitNetworkManagerSingleton();
        }
        catch (NetworkManagerNotFoundException ex)
        {
            CreateNetworkManager().SetSingleton();
        }
    }

    private static NetworkManager CreateNetworkManager()
    {
        GameObject networkManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NetworkManagerPrefabPath);
        networkManagerPrefab.name = "NetworkManager-RuntimeCreated";
        return networkManagerPrefab.GetComponent<NetworkManager>();
    }
}
