using System.Collections.Generic;
using System.Linq;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class MoveNotesToOtherVoiceAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    // The notes can be moved if there exists a note
    // that is not yet inside a voice with one of the given voice names.
    public bool CanMoveNotesToVoice(List<Note> selectedNotes, params EVoiceId[] voiceIds)
    {
        return selectedNotes.AnyMatch(note => !HasVoice(note, voiceIds));
    }

    public MovedNotesToVoiceEvent MoveNotesToVoice(SongMeta songMeta, List<Note> selectedNotes, EVoiceId voiceId, bool smartSplit = true)
    {
        if (smartSplit
            && ShouldSplit(songMeta, selectedNotes))
        {
            List<List<Note>> noteGroups = MoveNotesToOtherVoiceUtils.SplitIntoSentences(songMeta, selectedNotes);
            List<MoveNotesToOtherVoiceUtils.MoveNotesToVoiceResult> moveNotesToVoiceResults = noteGroups
                .Select(noteGroup =>
                {
                    MoveNotesToOtherVoiceUtils.MoveNotesToVoiceResult moveNotesToVoiceResult = MoveNotesToOtherVoiceUtils.MoveNotesToVoice(songMeta, noteGroup, voiceId);
                    return moveNotesToVoiceResult;
                })
                .ToList();

            return new MovedNotesToVoiceEvent(
                moveNotesToVoiceResults.SelectMany(it => it.Notes).ToList(),
                moveNotesToVoiceResults.SelectMany(it => it.ChangedSentences).ToList(),
                moveNotesToVoiceResults.SelectMany(it => it.RemovedSentences).ToList());
        }

        MoveNotesToOtherVoiceUtils.MoveNotesToVoiceResult moveNotesToVoiceResult = MoveNotesToOtherVoiceUtils.MoveNotesToVoice(songMeta, selectedNotes, voiceId);
        return new MovedNotesToVoiceEvent(
            moveNotesToVoiceResult.Notes,
            moveNotesToVoiceResult.ChangedSentences,
            moveNotesToVoiceResult.RemovedSentences);
    }

    private bool ShouldSplit(SongMeta songMeta, List<Note> selectedNotes)
    {
        // Split the notes into multiple sentences if needed
        bool hasDifferentSentences = selectedNotes.Select(note => note.Sentence).Distinct().Count() > 1;
        if (hasDifferentSentences)
        {
            return false;
        }

        int lengthInBeats = SongMetaUtils.LengthInBeats(selectedNotes);
        double lengthInMillis = SongMetaBpmUtils.MillisPerBeat(songMeta) * lengthInBeats;
        bool isVeryLong = lengthInMillis > 10000;
        return isVeryLong;
    }

    public void MoveNotesToVoiceAndNotify(SongMeta songMeta, List<Note> selectedNotes, EVoiceId voiceId)
    {
        MovedNotesToVoiceEvent movedNotesToVoiceEvent = MoveNotesToVoice(songMeta, selectedNotes, voiceId);
        songMetaChangeEventStream.OnNext(movedNotesToVoiceEvent);
    }

    private static bool HasVoice(Note note, EVoiceId[] voiceIds)
    {
        if (voiceIds.IsNullOrEmpty()
            || note == null)
        {
            return false;
        }
        return note.Sentence != null
               && note.Sentence.Voice != null
               && voiceIds.AnyMatch(voiceId => note.Sentence.Voice.Id == voiceId);
    }

    private class SentenceWithRange
    {
        public Sentence Sentence { get; private set; }
        public int FromBeat { get; private set; }
        public int UntilBeat { get; private set; }

        public SentenceWithRange(Sentence sentence, int fromBeat, int untilBeat)
        {
            Sentence = sentence;
            FromBeat = fromBeat;
            UntilBeat = untilBeat;
        }

        public bool ContainsBeatRange(int fromBeat, int untilBeat)
        {
            return FromBeat <= fromBeat
                   && untilBeat <= UntilBeat;
        }
    }
}
