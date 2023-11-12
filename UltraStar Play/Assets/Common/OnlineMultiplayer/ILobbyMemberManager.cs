using System.Collections.Generic;
using SteamOnlineMultiplayer;
using Unity.Netcode;

namespace CommonOnlineMultiplayer
{
    /**
     * Manages the connected clients of a hosted online game.
     * Therefor, does some bookkeeping of connected clients.
     */
    public interface ILobbyMemberManager
    {
        void OnNetcodeClientConnectionApproval(
            NetworkManager.ConnectionApprovalRequest connectionApprovalRequest,
            NetworkManager.ConnectionApprovalResponse response);
        void RemoveLobbyMemberFromRegistry(UnityNetcodeClientId netcodeClientId);
        void ClearLobbyMemberRegistry();
        IReadOnlyList<LobbyMember> GetLobbyMembers();
    }
}
