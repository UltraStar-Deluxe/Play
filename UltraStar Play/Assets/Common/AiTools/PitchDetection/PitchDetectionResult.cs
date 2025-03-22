public class PitchDetectionResult
{
    public string MidiFilePath { get; private set; }

    public PitchDetectionResult(string midiFilePath)
    {
        MidiFilePath = midiFilePath;
    }

    public override string ToString()
    {
        return $"{nameof(PitchDetectionResult)}(midiFilePath: {MidiFilePath})";
    }
}
