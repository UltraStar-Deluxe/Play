using UniRx;
using UniInject;
using System.Collections.Generic;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ChangeLinebreakBeatAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    public void AddToLinebreakBeat(IReadOnlyCollection<Sentence> selectedSentences, int amount)
    {
        foreach (Sentence sentence in selectedSentences)
        {
            sentence.UpdateMinAndMaxBeat();
            sentence.SetLinebreakBeat(sentence.LinebreakBeat + amount);
        }
    }

    public void AddToLinebreakBeatAndNotify(IReadOnlyCollection<Sentence> selectedSentences, int amount)
    {
        AddToLinebreakBeat(selectedSentences, amount);
        songMetaChangeEventStream.OnNext(new SentencesChangedEvent());
    }
}
