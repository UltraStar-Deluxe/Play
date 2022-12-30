public class VoskResultWordJson
{
    /**
     * The recognized word
     */
    public string word = "";

    /**
     * Confidence of the result
     */
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
