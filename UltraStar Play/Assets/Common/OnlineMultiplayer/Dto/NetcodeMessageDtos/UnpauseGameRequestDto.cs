namespace CommonOnlineMultiplayer
{
    public class UnpauseGameRequestDto : NetcodeRequestDto
    {
        public UnpauseGameRequestDto()
            : base(ENetcodeMessageType.UnpauseGameRequest)
        {
        }
    }
}
