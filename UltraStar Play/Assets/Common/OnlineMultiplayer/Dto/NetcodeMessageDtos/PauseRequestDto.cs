namespace CommonOnlineMultiplayer
{
    public class PauseRequestDto : NetcodeRequestDto
    {
        public bool ShowSenderName { get; set; } = true;
    }
}
