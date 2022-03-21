using System.Collections.Generic;

public class MovedNotesToVoiceEvent : SongMetaChangeEvent
{
    public IReadOnlyCollection<Note> Notes { get; private set; }
    public IReadOnlyCollection<Sentence> ChangedSentences { get; private set; }
    public IReadOnlyCollection<Sentence> RemovedSentences { get; private set; }

    public MovedNotesToVoiceEvent(IReadOnlyCollection<Note> notes,
        IReadOnlyCollection<Sentence> changedSentences,
        IReadOnlyCollection<Sentence> removedSentences)
    {
        this.Notes = notes;
        ChangedSentences = changedSentences;
        RemovedSentences = removedSentences;
    }
}
