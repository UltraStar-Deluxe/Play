using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;

namespace SteamOnlineMultiplayer.RequestHandlers
{
    public class CurrentSteamLobbyMembersRequestHandler : INetcodeRequestHandler
    {
        public int Priority => 1;
        public IReadOnlyList<ENetcodeMessageType> HandledMessageTypes => new List<ENetcodeMessageType>()
        {
            ENetcodeMessageType.CurrentLobbyMembersRequest
        };

        public string GetResponse(NetcodeRequestDto requestDto)
        {
            return new CurrentSteamLobbyMembersResponseDto()
            {
                SteamLobbyMembers = SteamMultiplayerManager.Instance.GetMembers().ToList()
            }.ToJson();
        }
    }
}
