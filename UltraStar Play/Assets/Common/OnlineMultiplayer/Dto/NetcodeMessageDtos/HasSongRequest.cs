namespace CommonOnlineMultiplayer
{
    public class HasSongRequest : NetcodeRequestDto
    {
        public string GloballyUniqueSongId { get; set; }

        public HasSongRequest()
            : base(ENetcodeMessageType.PlayerHasSongLocallyRequest)
        {
        }

        public HasSongRequest(string globallyUniqueSongId)
            : this()
        {
            GloballyUniqueSongId = globallyUniqueSongId;
        }
    }
}
