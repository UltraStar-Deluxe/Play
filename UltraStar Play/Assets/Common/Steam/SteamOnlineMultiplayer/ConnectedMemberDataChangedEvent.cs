namespace SteamOnlineMultiplayer
{
    public abstract class ConnectedMemberDataChangedEvent
    {
        public MemberData MemberData { get; private set; }

        protected ConnectedMemberDataChangedEvent(MemberData memberData)
        {
            MemberData = memberData;
        }
    }
}
