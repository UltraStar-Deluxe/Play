namespace CommonOnlineMultiplayer
{
    public class SuggestSongRequestDto : NetcodeRequestDto
    {
        public string GloballyUniqueSongId { get; set; }

        public SuggestSongRequestDto()
        {
        }

        public SuggestSongRequestDto(string globallyUniqueSongId)
            : base(ENetcodeMessageType.SuggestSongRequest)
        {
            GloballyUniqueSongId = globallyUniqueSongId;
        }
    }
}
