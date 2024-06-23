using System;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    [Serializable]
    public class LobbyMemberPlayerProfile : PlayerProfile
    {
        public UnityNetcodeClientId UnityNetcodeClientId { get; set; }
        public bool IsHost => UnityNetcodeClientId == NetworkManager.ServerClientId;
        public bool IsLocal => UnityNetcodeClientId == NetworkManager.Singleton.LocalClientId;
        public bool IsRemote => !IsLocal;

        public LobbyMemberPlayerProfile()
        {
        }

        public LobbyMemberPlayerProfile(string name, UnityNetcodeClientId unityNetcodeClientId)
            : base(name, EDifficulty.Medium)
        {
            UnityNetcodeClientId = unityNetcodeClientId;
        }

        public override string ToString()
        {
            return $"{nameof(LobbyMemberPlayerProfile)}(Name: {Name}, UnityNetcodeClientId: {UnityNetcodeClientId})";
        }
    }
}
