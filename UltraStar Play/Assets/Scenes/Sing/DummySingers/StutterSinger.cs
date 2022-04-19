using UnityEngine;

public class StutterSinger : AbstractDummySinger
{
    protected override BeatPitchEvent GetDummyPitchEvent(int beat)
    {
        Note noteAtBeat = GetNoteAtBeat(beat);
        if (noteAtBeat != null && Random.Range(0, 5) != 0)
        {
            return new BeatPitchEvent(noteAtBeat.MidiNote + Random.Range(-3, 3), beat);
        }

        return null;
    }
}
