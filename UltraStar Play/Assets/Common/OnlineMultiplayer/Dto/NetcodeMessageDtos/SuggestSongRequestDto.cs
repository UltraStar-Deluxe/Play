namespace CommonOnlineMultiplayer
{
    public class SuggestSongRequestDto : NetcodeRequestDto
    {
        public string GloballyUniqueSongId { get; set; }

        public SuggestSongRequestDto()
            : base(ENetcodeMessageType.SuggestSongRequest)
        {
        }

        public SuggestSongRequestDto(string globallyUniqueSongId)
            : this()
        {
            GloballyUniqueSongId = globallyUniqueSongId;
        }
    }
}
