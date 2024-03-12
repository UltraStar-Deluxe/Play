namespace CommonOnlineMultiplayer
{
    public class SingSceneReadyResponseDto : NetcodeResponseDto
    {
        public SingSceneReadyResponseDto()
            : base(ENetcodeMessageType.SingSceneReadyResponse)
        {
        }
    }
}
