namespace CommonOnlineMultiplayer
{
    public class SuggestSongRequestDto : NetcodeRequestDto
    {
        public string GloballyUniqueSongId { get; set; } = "";

        public SuggestSongRequestDto()
        {
        }

        public SuggestSongRequestDto(string globallyUniqueSongId)
        {
            GloballyUniqueSongId = globallyUniqueSongId;
        }
    }
}
