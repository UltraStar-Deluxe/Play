using System;
using UniInject;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ApplyBpmAndAdjustNoteLengthAction : INeedInjection
{
    [Inject]
    private SongMetaChangedEventStream songMetaChangedEventStream;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private SongMeta songMeta;

    public void Execute(double newBpm)
    {
        if (Math.Abs(newBpm - songMeta.BeatsPerMinute) < 0.01)
        {
            return;
        }

        if (newBpm <= 60)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason,
                "reason", "value too low"));
            return;
        }

        // Calculate start and end beat of all notes and sentences using the new bpm
        songMeta.Voices.ForEach(voice => AdjustNoteLength(voice, newBpm, songMeta.BeatsPerMinute));
        songMeta.BeatsPerMinute = newBpm;
    }

    private void AdjustNoteLength(Voice voice, double newBpm, double oldBpm)
    {
        voice.Sentences.ForEach(sentence => AdjustNoteLength(sentence, newBpm, oldBpm));
    }

    private void AdjustNoteLength(Sentence sentence, double newBpm, double oldBpm)
    {
        int newLinebreakBeat = (int)(sentence.LinebreakBeat * (newBpm / oldBpm));
        sentence.Notes.ForEach(note => AdjustNoteLength(note, newBpm, oldBpm));
        sentence.UpdateMinAndMaxBeat();
        sentence.SetLinebreakBeat(newLinebreakBeat);
    }

    private void AdjustNoteLength(Note note, double newBpm, double oldBpm)
    {
        int newStartBeat = (int)(note.StartBeat * (newBpm / oldBpm));
        int newEndBeat = (int)(note.EndBeat * (newBpm / oldBpm));
        if (newEndBeat < newStartBeat)
        {
            newEndBeat = newStartBeat + 1;
        }
        note.SetStartAndEndBeat(newStartBeat, newEndBeat);
    }

    public void ExecuteAndNotify(double newBpm)
    {
        Execute(newBpm);
        songMetaChangedEventStream.OnNext(new SongPropertyChangedEvent(ESongProperty.Bpm));
    }
}
