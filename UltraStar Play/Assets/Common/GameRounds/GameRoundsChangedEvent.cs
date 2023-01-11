public class GameRoundsChangedEvent
{
    public GameRoundData GameRoundData { get; private set; }

    public GameRoundsChangedEvent(GameRoundData gameRoundData)
    {
        GameRoundData = gameRoundData;
    }
}
