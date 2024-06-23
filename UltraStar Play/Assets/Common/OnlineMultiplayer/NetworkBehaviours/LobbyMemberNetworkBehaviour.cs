using CommonOnlineMultiplayer;
using Unity.Netcode;
using UnityEngine;

/**
 * An instance of this component is created for every client that joins a hosted game.
 * Therefor, this component is part of the prefab that is created by Unity NetworkManager when a new client connects.
 */
public class LobbyMemberNetworkBehaviour : NetworkBehaviour
{
    private readonly JsonSerializable4096BytesNetworkVariable<LobbyMember> lobbyMemberNetworkVariable = new();

    public string LobbyMemberJson => lobbyMemberNetworkVariable.Value.Value;
    public LobbyMember LobbyMember => lobbyMemberNetworkVariable.DeserializedValue;

    // Not used, only to see if Unity Netcode is working as expected
    private readonly NetworkVariable<Vector3> positionNetworkVariable = new NetworkVariable<Vector3>();

    private OnlineMultiplayerManager onlineMultiplayerManager;

    private void Awake()
    {
        onlineMultiplayerManager = OnlineMultiplayerManager.Instance;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"{nameof(LobbyMemberNetworkBehaviour)}.OnNetworkSpawn");

        // DontDestroyOnLoad object to persist the object across (custom implementation of) scene changes.
        DontDestroyOnLoad(this);

        if (IsServer)
        {
            // Distribute LobbyMember data, which is known on server, to all clients by setting the corresponding NetworkVariable.
            LobbyMember lobbyMember = onlineMultiplayerManager.LobbyMemberManager.GetLobbyMember(OwnerClientId);
            lobbyMemberNetworkVariable.DeserializedValue = lobbyMember;
        }

        onlineMultiplayerManager.OnLobbyMemberNetworkObjectSpawned(OwnerClientId);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        onlineMultiplayerManager.OnLobbyMemberNetworkObjectDestroyed(OwnerClientId);
    }

    private void Update()
    {
        transform.position = positionNetworkVariable.Value;

        UpdateGameObjectName();
    }

    private void UpdateGameObjectName()
    {
        if (!lobbyMemberNetworkVariable.Value.Value.IsNullOrEmpty())
        {
            name = $"{nameof(LobbyMemberNetworkBehaviour)}-{LobbyMember.UnityNetcodeClientId}-{LobbyMember.DisplayName}";
        }
    }

    public void Move()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Vector3 randomPosition = GetRandomPosition();
            transform.position = randomPosition;
            positionNetworkVariable.Value = randomPosition;
        }
        else
        {
            SubmitPositionRequestServerRpc();
        }
    }

    [ServerRpc]
    public void SubmitPositionRequestServerRpc(ServerRpcParams rpcParams = default)
    {
        positionNetworkVariable.Value = GetRandomPosition();
    }

    [ClientRpc]
    public void CustomParameterClientRpc(string aString, float aFloat)
    {
        Debug.Log("ClientRpcWithCustomParameters: " + aString + ", " + aFloat);
    }

    private static Vector3 GetRandomPosition()
    {
        return new Vector3(Random.Range(0f, 5f), 1f, Random.Range(0f, 5f));
    }
}
