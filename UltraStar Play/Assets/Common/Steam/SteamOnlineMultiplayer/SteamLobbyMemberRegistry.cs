using System;
using System.Collections.Generic;
using CommonOnlineMultiplayer;
using Steamworks;

namespace SteamOnlineMultiplayer
{
    [Serializable]
    public class SteamLobbyMemberRegistry
    {
        private readonly List<SteamLobbyMember> lobbyMembers = new();
        private readonly Dictionary<ulong, SteamLobbyMember> unityNetcodeClientIdToLobbyMember = new();
        private readonly Dictionary<SteamId, SteamLobbyMember> steamIdToLobbyMember = new();
        public int Count => lobbyMembers.Count;

        public void Clear()
        {
            lobbyMembers.Clear();
            unityNetcodeClientIdToLobbyMember.Clear();
            steamIdToLobbyMember.Clear();
        }

        public IReadOnlyList<SteamLobbyMember> GetAllLobbyMembers()
        {
            return lobbyMembers;
        }

        public void Add(SteamLobbyMember lobbyMember)
        {
            lobbyMembers.Add(lobbyMember);
            unityNetcodeClientIdToLobbyMember[lobbyMember.UnityNetcodeClientId] = lobbyMember;
            steamIdToLobbyMember[lobbyMember.SteamId] = lobbyMember;
        }

        public void Remove(SteamLobbyMember data)
        {
            lobbyMembers.Remove(data);
        }

        public bool TryGetDataBySteamId(SteamId steamId, out SteamLobbyMember lobbyMember)
        {
            return steamIdToLobbyMember.TryGetValue(steamId, out lobbyMember);
        }

        public bool TryGetDataByUnityNetcodeClientId(UnityNetcodeClientId netcodeClientId, out SteamLobbyMember lobbyMember)
        {
            return unityNetcodeClientIdToLobbyMember.TryGetValue(netcodeClientId, out lobbyMember);
        }
    }
}
