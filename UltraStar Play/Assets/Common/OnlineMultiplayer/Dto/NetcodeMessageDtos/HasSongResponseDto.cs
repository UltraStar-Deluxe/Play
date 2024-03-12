namespace CommonOnlineMultiplayer
{
    public class HasSongResponseDto : NetcodeResponseDto
    {
        public string GloballyUniqueSongId { get; private set; }
        public bool HasSong { get; private set; }

        public HasSongResponseDto()
            : base(ENetcodeMessageType.PlayerHasSongLocallyResponse)
        {
        }

        public HasSongResponseDto(string globallyUniqueSongId, bool hasSong)
            : this()
        {
            GloballyUniqueSongId = globallyUniqueSongId;
            HasSong = hasSong;
        }
    }
}
