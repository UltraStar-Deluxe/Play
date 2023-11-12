using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using SteamOnlineMultiplayer;
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
        SendRequestMessageToServerRpc(requestMessage, requestId);

        return responseSubject;
    }

    // ServerRpc => Executed on server
    [ServerRpc]
    private void SendRequestMessageToServerRpc(string requestMessage, string requestId)
    {
        Debug.Log($"Received request from client: {requestMessage}");

        string responseText = "";
        try
        {
            responseText = GetResponseText(requestMessage);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"Failed to process request from client, error message: {ex.Message}, request message: {requestMessage}");
        }

        SendResponseMessageToClientRpc(responseText, requestId);
    }

    private string GetResponseText(string requestMessage)
    {
        NetcodeRequestDto requestDto = JsonConverter.FromJson<NetcodeRequestDto>(requestMessage);
        INetcodeRequestHandler netcodeRequestHandler = NetcodeRequestHandlerRegistry.Instance.GetRequestHandler(requestDto.MessageType);
        return netcodeRequestHandler.GetResponse(requestDto);
    }

    // ClientRpc => Executed on client
    [ClientRpc]
    private void SendResponseMessageToClientRpc(string responseMessage, string requestId)
    {
        Debug.Log($"Received response from server: {responseMessage}");
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
            Debug.LogError($"Failed to find running request object for response from server with request id {requestId}. Response: {responseMessage}");
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
