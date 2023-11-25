namespace CommonOnlineMultiplayer
{
    public class HasSongResponse : NetcodeRequestDto
    {
        public string GloballyUniqueSongId { get; set; }
        public UnityNetcodeClientId UnityNetcodeClientId { get; set; }
        public bool HasSong { get; set; }

        public HasSongResponse()
            : base(ENetcodeMessageType.PlayerHasSongLocallyResponse)
        {
        }

        public HasSongResponse(string globallyUniqueSongId)
            : this()
        {
            GloballyUniqueSongId = globallyUniqueSongId;
        }
    }
}
