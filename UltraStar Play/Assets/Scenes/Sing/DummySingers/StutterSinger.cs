using UnityEngine;

public class StutterSinger : AbstractDummySinger
{
    protected override PitchEvent GetDummyPitchEvent(int beat, Note noteAtBeat)
    {
        PitchEvent pitchEvent = null;
        if (noteAtBeat != null && Random.Range(0, 5) != 0)
        {
            pitchEvent = new PitchEvent(noteAtBeat.MidiNote + Random.Range(-3, 3));
        }
        return pitchEvent;
    }
}
