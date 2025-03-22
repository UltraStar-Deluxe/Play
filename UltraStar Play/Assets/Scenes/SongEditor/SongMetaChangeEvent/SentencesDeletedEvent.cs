using System.Collections.Generic;

public class SentencesDeletedEvent : SongMetaChangedEvent
{
    public List<Sentence> Sentences { get; set; }
}
