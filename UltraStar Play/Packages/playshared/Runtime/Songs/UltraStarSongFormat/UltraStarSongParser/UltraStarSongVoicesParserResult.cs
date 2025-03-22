using System.Collections.Generic;

public class UltraStarSongVoicesParserResult
{
    public List<Voice> Voices { get; private set; }

    public UltraStarSongVoicesParserResult(List<Voice> voices)
    {
        Voices = voices;
    }
}
