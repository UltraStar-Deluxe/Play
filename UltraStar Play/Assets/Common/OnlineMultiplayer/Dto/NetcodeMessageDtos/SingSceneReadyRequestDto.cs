namespace CommonOnlineMultiplayer
{
    public class SingSceneReadyRequestDto : NetcodeRequestDto
    {
        public SingSceneReadyRequestDto()
            : base(ENetcodeMessageType.SingSceneReadyRequest)
        {
        }
    }
}
