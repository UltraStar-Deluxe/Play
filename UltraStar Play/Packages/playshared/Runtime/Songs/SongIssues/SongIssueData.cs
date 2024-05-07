public class SongIssueData
{
    public SongMeta SongMeta { get; private set; }

    public SongIssueData(SongMeta songMeta)
    {
        this.SongMeta = songMeta;
    }

    public override string ToString()
    {
        return $"{nameof(SongIssueData)}({SongMeta.GetArtistDashTitle()})";
    }
}
