using Steamworks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Network.Framework
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

        /// <summary>
        /// Checks if we are the owner (<b>NOTE:</b> If NetworkManager.Singleton is null, then owner is true. Helpful for Singleplayer)
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static bool IsOwner(this NetworkBehaviour obj) => NetworkManager.Singleton == null || obj.IsOwner;

        // /// <summary>
        // /// Check if scene exist in Network.SceneManager rather regular SceneManager, then loads it
        // /// </summary>
        // /// <param name="reference"></param>
        // /// <param name="loadMode"></param>
        // /// <returns></returns>
        // public static bool TryLoadNetworkScene(this SceneReference reference, LoadSceneMode loadMode = LoadSceneMode.Single)
        // {
        //     if (Application.CanStreamedLevelBeLoaded(reference.SceneName))
        //     {
        //         NetworkManager.Singleton.SceneManager.LoadScene(reference.SceneName, loadMode);
        //         return true;
        //     }
        //
        //     return false;
        // }
        //
        // /// <summary>
        // /// Check if scene exist in SceneManager, then loads it
        // /// </summary>
        // /// <param name="reference"></param>
        // /// <param name="loadMode"></param>
        // /// <returns></returns>
        // public static bool TryLoadRegularScene(this SceneReference reference, LoadSceneMode loadMode = LoadSceneMode.Single)
        // {
        //     if (Application.CanStreamedLevelBeLoaded(reference.SceneName))
        //     {
        //         SceneManager.LoadScene(reference.SceneName, loadMode);
        //         return true;
        //     }
        //
        //     return false;
        // }

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
