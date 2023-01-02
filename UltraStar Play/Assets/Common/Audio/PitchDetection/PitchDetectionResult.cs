using System.Collections.Generic;

public class PitchDetectionResult
{
    private readonly Dictionary<int, int> beatToMidiNote = new();

    public int MinBeat { get; private set; }
    public int MaxBeat { get; private set; }

    public bool IsEmpty => beatToMidiNote.Count <= 0;

    public void Add(int beat, int midiNote)
    {
        if (IsEmpty
            || beat < MinBeat)
        {
            MinBeat = beat;
        }
        if (IsEmpty
            || beat > MaxBeat)
        {
            MaxBeat = beat;
        }

        beatToMidiNote[beat] = midiNote;
    }

    public bool TryGetMidiNote(int beat, out int midiNote)
    {
        return beatToMidiNote.TryGetValue(beat, out midiNote);
    }
}
