public class VoskResultWordJson
{
    /**
     * The recognized word
     */
    public string word;

    /**
     * Confidence of the result
     */
    // TODO: Use confidence value to indicate which text need review in song editor
    public double conf;

    /**
     * Start of the word in the audio in seconds.
     */
    public double start;

    /**
     * End of the word in the audio in seconds.
     */
    public double end;
}
