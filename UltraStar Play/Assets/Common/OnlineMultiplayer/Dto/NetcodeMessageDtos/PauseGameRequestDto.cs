namespace CommonOnlineMultiplayer
{
    public class PauseGameRequestDto : NetcodeRequestDto
    {
        public PauseGameRequestDto()
            : base(ENetcodeMessageType.PauseGameRequest)
        {
        }
    }
}
