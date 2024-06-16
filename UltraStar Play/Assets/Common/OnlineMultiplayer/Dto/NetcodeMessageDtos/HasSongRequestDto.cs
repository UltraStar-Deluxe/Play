namespace CommonOnlineMultiplayer
{
    public class HasSongRequestDto : NetcodeRequestDto
    {
        public string GloballyUniqueSongId { get; private set; } = "";

        public HasSongRequestDto()
        {
        }

        public HasSongRequestDto(string globallyUniqueSongId)
        {
            GloballyUniqueSongId = globallyUniqueSongId;
        }
    }
}
