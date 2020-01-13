using UniRx;
using UniInject;
using System.Collections.Generic;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SentenceFitToNoteAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public void Execute(IReadOnlyCollection<Sentence> selectedSentences)
    {
        foreach (Sentence sentence in selectedSentences)
        {
            sentence.UpdateMinAndMaxBeat();
            sentence.SetLinebreakBeat(sentence.MaxBeat);
        }
    }

    public void ExecuteAndNotify(IReadOnlyCollection<Sentence> selectedSentences)
    {
        Execute(selectedSentences);
        songMetaChangeEventStream.OnNext(new SentencesChangedEvent());
    }
}