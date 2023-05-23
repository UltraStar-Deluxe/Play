using UnityEngine;

public class NearlyPerfectSinger : AbstractDummySinger
{
    [Range(0, 100)]
    public int randomness;
    
    public int noteOffset;
    
    [Range(0, 8)]
    public int maxNonPerfectOffset = 2;
    
    [Range(0, 8)]
    public int minBeatCount = 2;

    private bool singPerfectly = true;

    private int nonPerfectOffset;

    private int beatCount;

    protected override BeatPitchEvent GetDummyPitchEvent(int beat)
    {
        BeatPitchEvent result = null;
        Note noteAtBeat = GetNoteAtBeat(beat, true, false);
        if (noteAtBeat != null)
        {
            float frequency = 0;
            result = new BeatPitchEvent(noteAtBeat.MidiNote + noteOffset + nonPerfectOffset, beat, frequency);
            beatCount++;
            
            if (((!singPerfectly) 
                 || Random.Range(0, 100) > 100 - randomness) 
                && beatCount >= minBeatCount
                && beat < noteAtBeat.EndBeat - minBeatCount
                && beat > noteAtBeat.StartBeat)
            {
                singPerfectly = !singPerfectly;
                if (singPerfectly)
                {
                    nonPerfectOffset = 0;
                }
                else
                {
                    int factor = Random.Range(0, 2) == 0 ? -1 : 1;
                    nonPerfectOffset = Random.Range(2, maxNonPerfectOffset) * factor;
                }

                beatCount = 0;
            }
        }

        return result;
    }
}
