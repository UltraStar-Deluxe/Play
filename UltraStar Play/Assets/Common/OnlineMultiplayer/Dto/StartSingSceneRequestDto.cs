namespace CommonOnlineMultiplayer
{
    public class StartSingSceneRequestDto : NetcodeRequestDto
    {
        public SingSceneDataDto SingSceneDataDto { get; set; }

        public StartSingSceneRequestDto()
            : base(ENetcodeMessageType.StartSongRequest)
        {
        }
    }
}
