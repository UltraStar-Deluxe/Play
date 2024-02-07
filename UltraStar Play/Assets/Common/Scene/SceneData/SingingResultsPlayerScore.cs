public class SingingResultsPlayerScore : ISingingResultsPlayerScore
{
    public int NormalNotesTotalScore { get; set; }
    public int GoldenNotesTotalScore { get; set; }
    public int PerfectSentenceBonusTotalScore { get; set; }
    public int ModTotalScore { get; set; }

    public SingingResultsPlayerScore()
    {
    }

    public SingingResultsPlayerScore(ISingingResultsPlayerScore other)
    {
        if (other == null)
        {
            return;
        }

        NormalNotesTotalScore = other.NormalNotesTotalScore;
        GoldenNotesTotalScore = other.GoldenNotesTotalScore;
        PerfectSentenceBonusTotalScore = other.PerfectSentenceBonusTotalScore;
        ModTotalScore = other.ModTotalScore;
    }
}
