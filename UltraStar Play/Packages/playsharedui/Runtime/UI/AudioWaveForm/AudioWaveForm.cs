using System.Collections.Generic;

public class AudioWaveForm
{
    public List<AmplitudeRange> AmplitudeRanges { get; set; }

    public AudioWaveForm(List<AmplitudeRange> amplitudeRanges)
    {
        AmplitudeRanges = amplitudeRanges;
    }
}
