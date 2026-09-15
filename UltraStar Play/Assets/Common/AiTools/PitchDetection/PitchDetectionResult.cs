using System.Collections.Generic;

public class PitchDetectionResult
{
    public List<PitchDetectionResultNote> Notes { get; set; } = new();
}

public class PitchDetectionResultNote
{
    public double StartInMillis { get; set; }
    public double LengthInMillis { get; set; }
    public int MidiNote { get; set; }
    public float Confidence { get; set; }
}
