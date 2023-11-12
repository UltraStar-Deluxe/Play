using System;
using System.Collections.Generic;
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
    private void SendResponseMessageToClientRpc(string responseMessage, string requestId, UnityNetcodeClientId targetNetcodeClientId)
    {
        if (targetNetcodeClientId != OwnerClientId)
        {
            return;
        }

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

            try
            {
                runningRequestData.ResponseSubject.OnCompleted();
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

    private struct RunningRequestData
    {
        public string RequestId { get; set; }
        public string RequestMessage { get; set; }
        public string ResponseMessage { get; set; }
        public Subject<string> ResponseSubject { get; set; }
    }
}
