using System;
using UniInject;
using Unity.Netcode;
using UnityEngine;

namespace CommonOnlineMultiplayer
{
    public class NetcodeLobbyManager : AbstractSingletonBehaviour, INeedInjection, ILobbyManager
    {
        public static NetcodeLobbyManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<NetcodeLobbyManager>();

        [Inject]
        private NetworkManager networkManager;

        private bool isLeavingLobby;

        protected override object GetInstance()
        {
            return Instance;
        }

        public ILobby CurrentLobby
        {
            get
            {
                if (networkManager.IsClient)
                {
                    return new NetcodeLobby("Direct Connection");
                }

                return null;
            }
        }

        public void LeaveCurrentLobby()
        {
            networkManager.ShutdownIfConnectedClient("Leaving Netcode lobby");
        }

        private void LateUpdate()
        {
            isLeavingLobby = false;
        }

        public class NetcodeLobby : ILobby
        {
            private readonly string name;
            public string Name => name;

            public NetcodeLobby(string name)
            {
                this.name = name;
            }
        }
    }
}
