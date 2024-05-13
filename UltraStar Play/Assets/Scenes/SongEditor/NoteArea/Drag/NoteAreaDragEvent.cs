public class NoteAreaDragEvent
{
    public GeneralDragEvent GeneralDragEvent { get; private set; }

    public int MidiNoteDragStart { get; private set; }
    public int MidiNoteDistance { get; private set; }

    public int PositionInMillisDragStart { get; private set; }
    public int MillisDistance { get; private set; }

    public int PositionInBeatsDragStart { get; private set; }
    public int BeatDistance { get; private set; }

    public NoteAreaDragEvent(GeneralDragEvent generalDragEvent,
        int midiNoteDragStart, int midiNoteDistance,
        int positionInMillisDragStart, int millisDistance,
        int positionInBeatsDragStart, int beatDistance)
    {
        GeneralDragEvent = generalDragEvent;

        MidiNoteDragStart = midiNoteDragStart;
        MidiNoteDistance = midiNoteDistance;

        PositionInMillisDragStart = positionInMillisDragStart;
        MillisDistance = millisDistance;

        PositionInBeatsDragStart = positionInBeatsDragStart;
        BeatDistance = beatDistance;
    }
}
