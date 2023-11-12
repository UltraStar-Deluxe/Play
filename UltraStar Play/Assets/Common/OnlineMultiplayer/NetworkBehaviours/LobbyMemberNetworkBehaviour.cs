using Unity.Netcode;
using UnityEngine;

/**
 * An instance of this component is created for every client that joins a hosted game.
 * Therefor, this component is part of the prefab that is created by Unity NetworkManager when a new client connects.
 */
public class LobbyMemberNetworkBehaviour : NetworkBehaviour
{
    public LobbyMemberMessagingNetworkBehaviour MessagingNetworkBehaviour { get; private set; }

    // Not used, only to see if Unity Netcode is working as expected
    private readonly NetworkVariable<Vector3> positionNetworkVariable = new NetworkVariable<Vector3>();

    private void Awake()
    {
        MessagingNetworkBehaviour = GetComponentInChildren<LobbyMemberMessagingNetworkBehaviour>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"{nameof(LobbyMemberNetworkBehaviour)}.OnNetworkSpawn");

        // DontDestroyOnLoad object to persist the object across (custom implementation of) scene changes.
        DontDestroyOnLoad(this);

        if (IsOwner)
        {
            // This is executed only on the client that owns this object.
        }
    }

    private void Update()
    {
        transform.position = positionNetworkVariable.Value;
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
