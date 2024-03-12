namespace CommonOnlineMultiplayer
{
    public class EndSingSceneRequest : NetcodeRequestDto
    {
        public EndSingSceneRequest()
            : base(ENetcodeMessageType.EndSingSceneRequest)
        {
        }
    }
}
