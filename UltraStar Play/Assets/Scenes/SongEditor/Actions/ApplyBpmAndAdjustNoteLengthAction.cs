using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ApplyBpmAndAdjustNoteLengthAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private UiManager uiManager;

    [Inject]
    private SongMeta songMeta;

    public void Execute(float newBpm)
    {
        if (newBpm == songMeta.Bpm)
        {
            return;
        }

        if (newBpm <= 60)
        {
            uiManager.CreateNotification("New BPM is set much too low.");
            return;
        }

        // Calculate start and end beat of all notes and sentences using the new bpm
        songMeta.GetVoices().ForEach(voice => AdjustNoteLength(voice, newBpm, songMeta.Bpm));
        songMeta.Bpm = newBpm;
    }

    private void AdjustNoteLength(Voice voice, float newBpm, float oldBpm)
    {
        voice.Sentences.ForEach(sentence => AdjustNoteLength(sentence, newBpm, oldBpm));
    }

    private void AdjustNoteLength(Sentence sentence, float newBpm, float oldBpm)
    {
        int newLinebreakBeat = (int)(sentence.LinebreakBeat * (newBpm / oldBpm));
        sentence.Notes.ForEach(note => AdjustNoteLength(note, newBpm, oldBpm));
        sentence.UpdateMinAndMaxBeat();
        sentence.SetLinebreakBeat(newLinebreakBeat);
    }

    private void AdjustNoteLength(Note note, float newBpm, float oldBpm)
    {
        int newStartBeat = (int)(note.StartBeat * (newBpm / oldBpm));
        int newEndBeat = (int)(note.EndBeat * (newBpm / oldBpm));
        if (newEndBeat < newStartBeat)
        {
            newEndBeat = newStartBeat + 1;
        }
        note.SetStartAndEndBeat(newStartBeat, newEndBeat);
    }

    public void ExecuteAndNotify(float newBpm)
    {
        Execute(newBpm);
        songMetaChangeEventStream.OnNext(new BpmChangeEvent());
    }
}
