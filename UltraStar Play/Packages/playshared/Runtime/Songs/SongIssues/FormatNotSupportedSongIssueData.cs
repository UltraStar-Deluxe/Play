public class FormatNotSupportedSongIssueData : SongIssueData
{
    public enum EMediaType
    {
        Audio,
        Video,
        VocalsAudio,
        InstrumentalAudio,
    }

    public EMediaType MediaType { get; private set; }

    public FormatNotSupportedSongIssueData(SongMeta songMeta, EMediaType mediaType)
        : base(songMeta)
    {
        MediaType = mediaType;
    }

    public override string ToString()
    {
        return $"{nameof(FormatNotSupportedSongIssueData)}({MediaType}, {SongMeta.GetArtistDashTitle()})";
    }
}
