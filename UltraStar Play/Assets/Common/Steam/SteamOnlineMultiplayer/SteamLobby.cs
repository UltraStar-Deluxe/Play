using CommonOnlineMultiplayer;
using Steamworks;
using Steamworks.Data;

namespace SteamOnlineMultiplayer
{
    public class SteamLobby : ILobby
    {
        public Lobby Value { get; private set; }

        public SteamId Id => Value.Id;
        public string Name => Value.GetName();
        public string Password => Value.GetPassword();

        public SteamLobby(Lobby value)
        {
            Value = value;
        }

        public void Leave()
        {
            Value.Leave();
        }
    }
}
