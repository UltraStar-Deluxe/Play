public class SentenceAnalyzedEvent
{
    public Sentence Sentence { get; private set; }
    public bool IsLastSentence { get; private set; }

    public SentenceAnalyzedEvent(Sentence sentence, bool isLastSentence)
    {
        Sentence = sentence;
        IsLastSentence = isLastSentence;
    }
}
