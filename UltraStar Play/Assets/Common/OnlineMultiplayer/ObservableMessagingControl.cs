using System;
using System.Collections.Generic;
using UniRx;
using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public class ObservableMessagingControl
    {
        private NetworkManager NetworkManager => NetworkManager.Singleton;
        private readonly Func<IReadOnlyList<ulong>> otherLobbyMemberUnityNetcodeClientIdsGetter;
        private readonly Func<ulong> ownNetcodeClientIdGetter;

        public ObservableMessagingControl(
            Func<IReadOnlyList<ulong>> otherLobbyMemberUnityNetcodeClientIdsGetter,
            Func<ulong> ownNetcodeClientIdGetter)
        {
            this.otherLobbyMemberUnityNetcodeClientIdsGetter = otherLobbyMemberUnityNetcodeClientIdsGetter;
            this.ownNetcodeClientIdGetter = ownNetcodeClientIdGetter;
        }

        public void SendMessageToOtherClients(JsonSerializable jsonSerializable)
        {
            IReadOnlyList<ulong> otherLobbyMemberUnityNetcodeClientIds = otherLobbyMemberUnityNetcodeClientIdsGetter.Invoke();
            SendMessageToClients(jsonSerializable, otherLobbyMemberUnityNetcodeClientIds);
        }

        public void SendMessageToServer(JsonSerializable jsonSerializable)
        {
            SendRequestToServerAsObservable<object>(jsonSerializable)
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Exception when sending message to server: {ex.Message}. Sent message: {jsonSerializable.ToJson()}");
                })
                // Subscribe to trigger observable
                .Subscribe(responseDto =>
                {
                    // Do nothing.
                });
        }

        public IObservable<RESPONSEDTO> SendRequestToServerAsObservable<RESPONSEDTO>(JsonSerializable jsonSerializable)
            where RESPONSEDTO : new()
        {
            if (jsonSerializable == null)
            {
                return Observable.Empty<RESPONSEDTO>();
            }

            NetworkObject localPlayerObject = NetworkManager.SpawnManager.GetLocalPlayerObject();
            if (localPlayerObject == null)
            {
                throw new OnlineMultiplayerException("Missing LocalPlayerObject");
            }

            LobbyMemberNetworkBehaviour lobbyMemberNetworkBehaviour = localPlayerObject.GetComponent<LobbyMemberNetworkBehaviour>();
            if (lobbyMemberNetworkBehaviour == null)
            {
                throw new OnlineMultiplayerException($"Missing {nameof(LobbyMemberMessagingNetworkBehaviour)}");
            }

            return lobbyMemberNetworkBehaviour.MessagingNetworkBehaviour
                .SendRequestToServerAsObservable(jsonSerializable.ToJson())
                .Select(responseMessage => JsonConverter.FromJson<RESPONSEDTO>(responseMessage));
        }

        public IObservable<RESPONSEDTO> SendMessageToClientsAsObservable<RESPONSEDTO>(
            JsonSerializable jsonSerializable,
            IReadOnlyList<ulong> netcodeClientIds)
            where RESPONSEDTO : new()
        {
            if (jsonSerializable == null
                || netcodeClientIds.IsNullOrEmpty())
            {
                return Observable.Empty<RESPONSEDTO>();
            }

            NetworkObject localPlayerObject = NetworkManager.SpawnManager.GetLocalPlayerObject();
            if (localPlayerObject == null)
            {
                throw new OnlineMultiplayerException("Missing LocalPlayerObject");
            }

            LobbyMemberNetworkBehaviour lobbyMemberNetworkBehaviour = localPlayerObject.GetComponent<LobbyMemberNetworkBehaviour>();
            if (lobbyMemberNetworkBehaviour == null)
            {
                throw new OnlineMultiplayerException($"Missing {nameof(LobbyMemberMessagingNetworkBehaviour)}");
            }

            return lobbyMemberNetworkBehaviour.MessagingNetworkBehaviour
                .SendRequestMessageToAllClientsAsObservable(jsonSerializable.ToJson(), netcodeClientIds)
                .Select(responseMessage => JsonConverter.FromJson<RESPONSEDTO>(responseMessage));
        }

        public void SendMessageToAllClients(JsonSerializable jsonSerializable)
        {
            List<ulong> allClientIds = new(otherLobbyMemberUnityNetcodeClientIdsGetter.Invoke());
            allClientIds.Add(ownNetcodeClientIdGetter.Invoke());
            SendMessageToClients(jsonSerializable, allClientIds);
        }

        public void SendMessageToClients(
            JsonSerializable jsonSerializable,
            IReadOnlyList<ulong> netcodeClientIds)
        {
            SendMessageToClientsAsObservable<object>(jsonSerializable, netcodeClientIds)
                .CatchIgnore((Exception ex) =>
                {
                    Debug.LogException(ex);
                    Debug.LogError($"Failed to send message to clients: {ex.Message}. Message to be sent: {jsonSerializable.ToJson()}");
                })
                // Subscribe to trigger observable
                .Subscribe(responseDto =>
                {
                    // Do nothing.
                });
        }
    }
}
