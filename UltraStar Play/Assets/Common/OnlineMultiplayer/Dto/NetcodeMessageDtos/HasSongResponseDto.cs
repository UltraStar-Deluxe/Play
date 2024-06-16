namespace CommonOnlineMultiplayer
{
    public class HasSongResponseDto : NetcodeResponseDto
    {
        public string GloballyUniqueSongId { get; private set; } = "";
        public bool HasSong { get; private set; }

        public HasSongResponseDto()
        {
        }

        public HasSongResponseDto(string globallyUniqueSongId, bool hasSong)
        {
            GloballyUniqueSongId = globallyUniqueSongId;
            HasSong = hasSong;
        }
    }
}
