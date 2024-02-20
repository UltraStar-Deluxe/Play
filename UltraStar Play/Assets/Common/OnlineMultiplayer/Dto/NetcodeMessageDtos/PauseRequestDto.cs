namespace CommonOnlineMultiplayer
{
    public class PauseRequestDto : NetcodeRequestDto
    {
        public bool ShowSenderName { get; set; } = true;

        public PauseRequestDto()
            : base(ENetcodeMessageType.PauseRequest)
        {
        }
    }
}
