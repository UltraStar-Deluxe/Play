public class SingingResultsPlayerScore : ISingingResultsPlayerScore
{
    public int NormalNotesTotalScore { get; set; }
    public int GoldenNotesTotalScore { get; set; }
    public int PerfectSentenceBonusTotalScore { get; set; }
    public int ModTotalScore { get; set; }

    public int TotalScore => NormalNotesTotalScore
                             + GoldenNotesTotalScore
                             + PerfectSentenceBonusTotalScore
                             + ModTotalScore;
}
