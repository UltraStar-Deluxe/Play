namespace CommonOnlineMultiplayer
{
    public class HasSongRequestDto : NetcodeRequestDto
    {
        public string GloballyUniqueSongId { get; private set; }

        public HasSongRequestDto()
            : base(ENetcodeMessageType.PlayerHasSongLocallyRequest)
        {
        }

        public HasSongRequestDto(string globallyUniqueSongId)
            : this()
        {
            GloballyUniqueSongId = globallyUniqueSongId;
        }
    }
}
