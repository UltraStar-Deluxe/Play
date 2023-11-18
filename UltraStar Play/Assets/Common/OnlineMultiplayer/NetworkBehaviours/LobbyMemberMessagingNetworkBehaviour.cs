using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using UniRx;
using Unity.Netcode;
using UnityEngine;

public class LobbyMemberMessagingNetworkBehaviour : NetworkBehaviour
{
    private readonly Dictionary<string, RunningRequestData> requestIdToRunningRequestData = new();

    public IObservable<string> SendRequestToServerAsObservable(string requestMessage)
    {
        Subject<string> responseSubject = new Subject<string>();

        string requestId = Guid.NewGuid().ToString();
        RunningRequestData runningRequestData = new()
        {
            RequestId = requestId,
            RequestMessage = requestMessage,
            ResponseSubject = responseSubject,
        };
        requestIdToRunningRequestData[requestId] = runningRequestData;

        Debug.Log($"Sending request to server: {requestMessage}, requestId: {requestId}, senderNetcodeClientId: {OwnerClientId}");
        SendRequestMessageToServerRpc(requestMessage, requestId, OwnerClientId);

        return responseSubject;
    }

    public IObservable<string> SendRequestMessageToAllClientsAsObservable(
        string requestMessage,
        List<UnityNetcodeClientId> unityNetcodeClientIds)
    {
        if (requestMessage.IsNullOrEmpty()
            || unityNetcodeClientIds.IsNullOrEmpty())
        {
            return Observable.Empty<string>();
        }

        Subject<string> responseSubject = new Subject<string>();

        string requestId = Guid.NewGuid().ToString();
        RunningRequestData runningRequestData = new()
        {
            RequestId = requestId,
            RequestMessage = requestMessage,
            ResponseSubject = responseSubject,
        };
        requestIdToRunningRequestData[requestId] = runningRequestData;

        Debug.Log($"Sending request to {unityNetcodeClientIds.Count} clients: {requestMessage}, requestId: {requestId}, senderNetcodeClientId: {OwnerClientId}");
        List<ulong> unityNetcodeClientIdsAsLongList = unityNetcodeClientIds
            .Select(it => it.Value)
            .ToList();
        ClientRpcParams clientRpcParams = new()
        {
            Send = new ClientRpcSendParams()
            {
                TargetClientIds = unityNetcodeClientIdsAsLongList,
            }
        };
        SendRequestMessageToClientRpc(requestMessage, requestId, OwnerClientId, clientRpcParams);

        return responseSubject;
    }

    // ServerRpc => Executed on server
    [ServerRpc]
    private void SendRequestMessageToServerRpc(string requestMessage, string requestId, UnityNetcodeClientId senderNetcodeClientId)
    {
        Debug.Log($"Received request from client: {requestMessage}, requestId: {requestId}, senderNetcodeClientId: {senderNetcodeClientId}");

        string responseText = "";
        try
        {
            responseText = GetResponseText(new NetcodeRequest(requestMessage, senderNetcodeClientId));
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to process request from client, error message: {ex.Message}, request message: {requestMessage}, requestId: {requestId}, senderNetcodeClientId: {senderNetcodeClientId}");
        }

        SendResponseMessageToClientRpc(responseText, requestId, senderNetcodeClientId);
    }

    private string GetResponseText(NetcodeRequest netcodeRequest)
    {
        NetcodeRequestDto requestDto = JsonConverter.FromJson<NetcodeRequestDto>(netcodeRequest.RequestMessage);
        INetcodeRequestHandler netcodeRequestHandler = NetcodeRequestHandlerRegistry.Instance.GetRequestHandler(requestDto.MessageType);
        return netcodeRequestHandler.GetResponse(netcodeRequest);
    }

    // ClientRpc => Executed on client
    [ClientRpc]
    private void SendResponseMessageToClientRpc(string responseMessage, string requestId, UnityNetcodeClientId targetNetcodeClientId, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"Received response from server: {responseMessage}, requestId: {requestId}, targetNetcodeClientId: {targetNetcodeClientId}");
        if (requestIdToRunningRequestData.TryGetValue(requestId, out RunningRequestData runningRequestData))
        {
            runningRequestData.ResponseMessage = responseMessage;
            requestIdToRunningRequestData.Remove(requestId);
            try
            {
                runningRequestData.ResponseSubject.OnNext(responseMessage);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        else
        {
            Debug.LogError($"Failed to find running request object for response from server with requestId {requestId}. Response: {responseMessage}");
        }
    }

    [ClientRpc]
    private void SendRequestMessageToClientRpc(string requestMessage, string requestId, UnityNetcodeClientId senderNetcodeClientId, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"Received request from server: {requestMessage}, requestId: {requestId}, senderNetcodeClientId: {senderNetcodeClientId}");

        string responseText = "";
        try
        {
            responseText = GetResponseText(new NetcodeRequest(requestMessage, senderNetcodeClientId));
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to process request from server, error message: {ex.Message}, request message: {requestMessage}, requestId: {requestId}, senderNetcodeClientId: {senderNetcodeClientId}");
        }

        SendResponseMessageToServerRpc(responseText, requestId, senderNetcodeClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SendResponseMessageToServerRpc(string responseMessage, string requestId, UnityNetcodeClientId targetNetcodeClientId, ServerRpcParams serverRpcParams = default)
    {
        Debug.Log($"Received response from client: {responseMessage}, requestId: {requestId}, targetNetcodeClientId: {targetNetcodeClientId}");
        if (requestIdToRunningRequestData.TryGetValue(requestId, out RunningRequestData runningRequestData))
        {
            runningRequestData.ResponseMessage = responseMessage;
            requestIdToRunningRequestData.Remove(requestId);
            try
            {
                runningRequestData.ResponseSubject.OnNext(responseMessage);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        else
        {
            Debug.LogError($"Failed to find running request object for response from client with requestId {requestId}. Response: {responseMessage}");
        }
    }

    private struct RunningRequestData
    {
        public string RequestId { get; set; }
        public string RequestMessage { get; set; }
        public string ResponseMessage { get; set; }
        public Subject<string> ResponseSubject { get; set; }
    }
}
