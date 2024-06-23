using System.Collections.Generic;

public class MergedVoice : Voice
{
    public List<Voice> OriginalVoices { get; private set; }

    public MergedVoice(List<Voice> originalVoices)
    {
        OriginalVoices = originalVoices;
    }
}
