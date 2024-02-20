namespace CommonOnlineMultiplayer
{
    public class SingingResultsPlayerScoreRequestDto : NetcodeRequestDto
    {
        public ISingingResultsPlayerScore SingingResultsPlayerScore { get; set; }

        public SingingResultsPlayerScoreRequestDto()
            : base(ENetcodeMessageType.SingingResultsPlayerScoreRequest)
        {
        }
    }
}
