using System.Collections.Generic;

public class SentencesDeletedEvent : ISongMetaChangeEvent
{
    private IReadOnlyCollection<Sentence> sentences;

    public SentencesDeletedEvent(IReadOnlyCollection<Sentence> sentences)
    {
        this.sentences = sentences;
    }
}