public class AudioSeparationResult
{
    public string OriginalAudioPath { get; private set; }
    public string VocalsAudioPath { get; private set; }
    public string InstrumentalAudioPath { get; private set; }

    public AudioSeparationResult(string originalAudioPath, string vocalsAudioPath, string instrumentalAudioPath)
    {
        OriginalAudioPath = originalAudioPath;
        VocalsAudioPath = vocalsAudioPath;
        InstrumentalAudioPath = instrumentalAudioPath;
    }

    public override string ToString()
    {
        return $"{nameof(AudioSeparationResult)}(original: '{OriginalAudioPath}', vocals: '{VocalsAudioPath}', instrumental: '{InstrumentalAudioPath}')";
    }
}
