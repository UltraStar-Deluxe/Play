using Steamworks.Data;

namespace SteamOnlineMultiplayer
{
    public static class LobbyExtensions
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

        public static void WithPassword(this LobbyQuery lobbyQuery, string value)
        {
            lobbyQuery.WithKeyValue("password", value);
        }
    }
}
