using Steamworks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace SteamOnlineMultiplayer
{
    public static class SteamMultiplayerNetworkExtensions
    {
        public enum ConnectStatus : byte
        {
            Undefined,
            Success,
            ServerFull,
            GameInProgress,
            LoggedInAgain,
            UserRequestedDisconnect,
            GenericDisconnect,
            KickDisconnect,
            BanDisconnect,
        }

        [System.Serializable]
        public class ConnectionPayload
        {
            public int clientScene = -1;
            public FixedString32Bytes clientGUID;
            public FixedString32Bytes displayName;
        }

        public struct SteamIdSerializable : INetworkSerializable
        {
            public SteamId steamId;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter => serializer.SerializeValue(ref steamId.Value);

            public static implicit operator SteamIdSerializable(SteamId value)
            {
                var result = default(SteamIdSerializable);
                result.steamId = value;
                return result;
            }
        }

        public struct MemberData
        {
            public SteamIdSerializable SteamIdData { get; private set; }
            public FixedString32Bytes DisplayName { get; private set; }
            public ulong ClientId { get; private set; }

            public MemberData(SteamIdSerializable steamId, FixedString32Bytes playerName, ulong clientId)
            {
                SteamIdData = steamId;
                DisplayName = playerName;
                ClientId = clientId;
            }
        }

        public class DisconnectReason
        {
            public event UnityAction<ConnectStatus> OnReasonChanged;

            public ConnectStatus Reason { get; private set; } = ConnectStatus.Undefined;

            public void SetDisconnectReason(ConnectStatus reason)
            {
                Debug.Log($"New reason: {reason}");
                Reason = reason;
                OnReasonChanged?.Invoke(reason);
            }

            public void Clear() => Reason = ConnectStatus.Undefined;

            public bool HasTransitionReason => Reason != ConnectStatus.Undefined;
        }

        /**
         * Checks if we are the owner
         * NOTE: If NetworkManager.Singleton is null, then owner is true (helpful for single player)
         */
        public static bool IsOwner(this NetworkBehaviour obj) => NetworkManager.Singleton == null || obj.IsOwner;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Init()
        {
            SteamMultiplayerNetworkManager.OnDisconnectReceived += OnDisconnectReceived;
            Application.quitting += Shutdown;
        }

        private static void Shutdown()
        {
            Application.quitting -= Shutdown;
            SteamMultiplayerNetworkManager.OnDisconnectReceived -= OnDisconnectReceived;
        }

        private static void OnDisconnectReceived(ConnectStatus status) => Debug.Log($"Disconnect: {status}");
    }
}
