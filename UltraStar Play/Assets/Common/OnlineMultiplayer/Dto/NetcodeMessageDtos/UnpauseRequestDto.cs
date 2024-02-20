namespace CommonOnlineMultiplayer
{
    public class UnpauseRequestDto : NetcodeRequestDto
    {
        public bool ShowSenderName { get; set; } = true;

        public UnpauseRequestDto()
            : base(ENetcodeMessageType.UnpauseRequest)
        {
        }
    }
}
