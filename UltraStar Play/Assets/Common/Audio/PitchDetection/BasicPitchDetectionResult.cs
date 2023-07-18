public class BasicPitchDetectionResult
{
    public string MidiFilePath { get; private set; }

    public BasicPitchDetectionResult(string midiFilePath)
    {
        MidiFilePath = midiFilePath;
    }

    public override string ToString()
    {
        return $"{nameof(BasicPitchDetectionResult)}(midiFilePath: {MidiFilePath})";
    }
}
