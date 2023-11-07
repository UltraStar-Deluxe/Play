namespace SteamOnlineMultiplayer
{
    public class ConnectedMemberDataAddedEvent : ConnectedMemberDataChangedEvent
    {
        public ConnectedMemberDataAddedEvent(MemberData memberData)
            : base(memberData)
        {
        }
    }
}
