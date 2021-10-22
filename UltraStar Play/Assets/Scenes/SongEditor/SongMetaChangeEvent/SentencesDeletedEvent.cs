using System.Collections.Generic;

public class SentencesDeletedEvent : SongMetaChangeEvent
{
    public List<Sentence> Sentences { get; set; }
}
