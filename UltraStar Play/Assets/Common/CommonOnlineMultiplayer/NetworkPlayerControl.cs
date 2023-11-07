using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

/**
 * An instance of this component is created for every client that joins a hosted game.
 * Therefor, this component is part of the prefab that is created by Unity NetworkManager when a new client connects.
 */
public class NetworkPlayerControl : NetworkBehaviour
{
    private readonly NetworkVariable<Vector3> positionNetworkVariable = new NetworkVariable<Vector3>();
    // private readonly NetworkVariable<List<MemberData>> memberDatasNetworkVariable = new NetworkVariable<List<MemberData>>();

    private readonly Dictionary<string, RunningRequestData> requestIdToRunningRequestData = new();

    public override void OnNetworkSpawn()
    {
        Debug.Log($"{nameof(NetworkPlayerControl)}.OnNetworkSpawn");

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

    public IObservable<string> SendRequestToServerAsObservable(string requestMessage)
    {
        Subject<string> responseSubject = new Subject<string>();

        string requestId = Guid.NewGuid().ToString();
        RunningRequestData runningRequestData = new()
        {
            requestId = requestId,
            requestMessage = requestMessage,
            responseSubject = responseSubject,
        };
        requestIdToRunningRequestData[requestId] = runningRequestData;
        SendRequestMessageToServerRpc(requestMessage, requestId);

        return responseSubject;
    }

    // ServerRpc => Executed on server
    [ServerRpc]
    private void SendRequestMessageToServerRpc(string requestMessage, string requestId)
    {
        Debug.Log($"Received request from client: {requestMessage}");

        string responseMessage = "";
        try
        {
            responseMessage = GetResponseMessage(requestMessage);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to process request from client, error message: {ex.Message}, request message: {requestMessage}");
        }

        SendResponseMessageToClientRpc(responseMessage, requestId);
    }

    private string GetResponseMessage(string requestMessage)
    {
        NetcodeCustomMessageDto netcodeCustomMessageDto = JsonConverter.FromJson<NetcodeCustomMessageDto>(requestMessage);
        switch (netcodeCustomMessageDto.MessageType)
        {
            case EOnlineMultiplayerMessageType.MemberDatasRequest:
                return new ConnectedMemberDatasResponseDto()
                    {
                        MemberDatas = SteamOnlineMultiplayer.SteamMultiplayerManager.Instance.GetMembers().ToList()
                    }.ToJson();
        }

        throw new OnlineMultiplayerException($"Unhandled message type: {netcodeCustomMessageDto.MessageType}");
    }

    // ClientRpc => Executed on client
    [ClientRpc]
    private void SendResponseMessageToClientRpc(string responseMessage, string requestId)
    {
        Debug.Log($"Received response from server: {responseMessage}");
        if (requestIdToRunningRequestData.TryGetValue(requestId, out RunningRequestData runningRequestData))
        {
            runningRequestData.responseMessage = responseMessage;
            requestIdToRunningRequestData.Remove(requestId);
            try
            {
                runningRequestData.responseSubject.OnNext(responseMessage);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            try
            {
                runningRequestData.responseSubject.OnCompleted();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        else
        {
            Debug.LogError($"Failed to find running request object for response from server with request id {requestId}. Response: {responseMessage}");
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

    private struct RunningRequestData
    {
        public string requestId;
        public string requestMessage;
        public string responseMessage;
        public Subject<string> responseSubject;
    }
}
