using NHyphenator;

public class CreateNotesFromSpeechRecognitionConfig
{
    public SpeechRecognizerConfig SpeechRecognizerConfig { get; set; }
    public SpeechRecognitionInputSamples InputSamples { get; set; }

    /**
     * MIDI note used for the created notes.
     */
    public int MidiNote { get; set; }

    /**
     * SongMeta to determine note positions (beats and milliseconds).
     */
    public SongMeta SongMeta { get; set; }

    /**
     * Offset to shift created notes.
     */
    public int OffsetInBeats { get; set; }

    /**
     * Hyphenator used to split syllables
     */
    public Hyphenator Hyphenator { get; set; }

    /**
     * Space to add between notes.
     * This space is also added when splitting syllables.
     */
    public int SpaceInMillisBetweenNotes { get; set; }
}
