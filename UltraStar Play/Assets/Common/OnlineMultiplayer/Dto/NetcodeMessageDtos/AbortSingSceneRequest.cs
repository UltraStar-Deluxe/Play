namespace CommonOnlineMultiplayer
{
    public class AbortSingSceneRequest : NetcodeRequestDto
    {
        public AbortSingSceneRequest()
            : base(ENetcodeMessageType.AbortSingSceneRequest)
        {
        }
    }
}
