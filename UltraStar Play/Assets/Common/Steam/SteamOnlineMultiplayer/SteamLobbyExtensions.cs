using Steamworks.Data;

namespace SteamOnlineMultiplayer
{
    public static class SteamLobbyExtensions
    {
        public static string GetName(this Lobby lobby)
        {
            return lobby.GetData("name");
        }

        public static void SetName(this Lobby lobby, string value)
        {
            lobby.SetData("name", value);
        }

        public static void WithName(this LobbyQuery lobbyQuery, string value)
        {
            lobbyQuery.WithKeyValue("name", value);
        }

        public static string GetPassword(this Lobby lobby)
        {
            return lobby.GetData("password");
        }

        public static void SetPassword(this Lobby lobby, string value)
        {
            lobby.SetData("password", value);
        }

        public static LobbyQuery WithPassword(this LobbyQuery lobbyQuery, string value)
        {
            return lobbyQuery.WithKeyValue("password", value);
        }

        public static void SetVisibility(this Lobby lobby, ESteamLobbyVisibility visibility)
        {
            switch (visibility)
            {
                case ESteamLobbyVisibility.Public:
                    lobby.SetPublic();
                    break;
                case ESteamLobbyVisibility.Private:
                    lobby.SetPrivate();
                    break;
                case ESteamLobbyVisibility.FriendsOnly:
                    lobby.SetFriendsOnly();
                    break;
                case ESteamLobbyVisibility.Invisible:
                    lobby.SetInvisible();
                    break;
            }
        }
    }
}
