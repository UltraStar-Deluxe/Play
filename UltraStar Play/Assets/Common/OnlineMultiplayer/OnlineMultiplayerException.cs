using System;

namespace CommonOnlineMultiplayer
{
    public class OnlineMultiplayerException : Exception
    {
        public OnlineMultiplayerException(string message) : base(message)
        {
        }

        public OnlineMultiplayerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
