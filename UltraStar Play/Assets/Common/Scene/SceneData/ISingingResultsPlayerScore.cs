public interface ISingingResultsPlayerScore
{
    public int NormalNotesTotalScore { get; }
    public int GoldenNotesTotalScore { get; }
    public int PerfectSentenceBonusTotalScore { get; }
    public int ModTotalScore { get; }

    public int TotalScore => NormalNotesTotalScore
                             + GoldenNotesTotalScore
                             + PerfectSentenceBonusTotalScore
                             + ModTotalScore;
}
