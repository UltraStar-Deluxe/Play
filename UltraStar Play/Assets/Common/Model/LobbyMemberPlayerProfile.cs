using System;
using CommonOnlineMultiplayer;
using Unity.Netcode;

[Serializable]
public class LobbyMemberPlayerProfile : PlayerProfile
{
    public UnityNetcodeClientId UnityNetcodeClientId { get; set; }
    public bool IsHost => UnityNetcodeClientId == NetworkManager.ServerClientId;

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
