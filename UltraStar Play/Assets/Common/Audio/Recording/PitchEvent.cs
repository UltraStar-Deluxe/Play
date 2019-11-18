public struct PitchEvent
{
    public int MidiNote { get; set; }

    public PitchEvent(int midiNote)
    {
        MidiNote = midiNote;
    }
}