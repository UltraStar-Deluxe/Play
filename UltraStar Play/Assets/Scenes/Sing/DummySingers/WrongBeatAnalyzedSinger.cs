using UniInject;
using UnityEngine;

/**
 * Simulate pitch events on wrong beats.
 * Wrong analyzed beats may happen because of latency when using the Companion App.
 */
public class WrongBeatAnalyzedSinger : AbstractDummySinger
{
    [InjectedInInspector]
    [Range(-10, 10)]
    public int minOffsetInclusive = 0;
    
    [InjectedInInspector]
    [Range(-10, 10)]
    public int maxOffsetExclusive = 5;

    [InjectedInInspector]
    [Range(-10, 10)]
    public int beatToAnalyzeOffset;
    
    [InjectedInInspector]
    public bool stutter;

    protected override int BeatToAnalyze => playerControl.PlayerMicPitchTracker.BeatToAnalyze + beatToAnalyzeOffset;

    protected override BeatPitchEvent GetDummyPitchEvent(int beat)
    {
        Note noteAtBeat = GetNoteAtBeat(beat, true, false);
        if (noteAtBeat == null)
        {
            return null;
        }
        
        if (stutter && Random.Range(0, 5) == 0)
        {
            return null;
        }
        
        float frequency = 0;
        int simulatedBeat = beat + Random.Range(minOffsetInclusive, maxOffsetExclusive);
        return new BeatPitchEvent(noteAtBeat.MidiNote, simulatedBeat, frequency);
    }
}
