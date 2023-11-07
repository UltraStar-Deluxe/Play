namespace SteamOnlineMultiplayer
{
    public class ConnectedMemberDataRemovedEvent : ConnectedMemberDataChangedEvent
    {
        public ConnectedMemberDataRemovedEvent(MemberData memberData)
            : base(memberData)
        {
        }
    }
}
