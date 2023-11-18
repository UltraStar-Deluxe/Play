namespace CommonOnlineMultiplayer
{
    public class EmptyNetcodeResponseDto : NetcodeResponseDto
    {
        public static JsonSerializable Instance { get; private set; } = new EmptyNetcodeResponseDto();

        private EmptyNetcodeResponseDto()
            : base(ENetcodeMessageType.Other)
        {
        }
    }
}
