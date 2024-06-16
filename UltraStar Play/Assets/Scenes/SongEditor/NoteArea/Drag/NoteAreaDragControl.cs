using UniInject;
using UnityEngine;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class NoteAreaDragControl : AbstractDragControl<NoteAreaDragEvent>
{
    [Inject]
    private NoteAreaControl noteAreaControl;

    protected override NoteAreaDragEvent CreateDragEventStart(DragControlPointerEvent eventData)
    {
        GeneralDragEvent generalDragEvent = CreateGeneralDragEventStart(eventData);

        int midiNoteDragStart = noteAreaControl.ScreenPixelPositionToMidiNote(generalDragEvent.ScreenCoordinateInPixels.StartPosition.y);
        int midiNoteDistance = 0;

        int positionInMillisDragStart = noteAreaControl.ScreenPixelPositionToMillis(generalDragEvent.ScreenCoordinateInPixels.StartPosition.x);
        int millisDistance = 0;

        int positionInBeatsDragStart = noteAreaControl.ScreenPixelPositionToBeat(generalDragEvent.ScreenCoordinateInPixels.StartPosition.x);
        int beatDistance = 0;

        NoteAreaDragEvent result = new(generalDragEvent,
            midiNoteDragStart, midiNoteDistance,
            positionInMillisDragStart, millisDistance,
            positionInBeatsDragStart, beatDistance);

        return result;
    }

    protected override NoteAreaDragEvent CreateDragEvent(DragControlPointerEvent eventData, NoteAreaDragEvent dragStartEvent)
    {
        GeneralDragEvent generalDragEvent = CreateGeneralDragEvent(eventData, dragStartEvent.GeneralDragEvent);

        int midiNoteDragStart = dragStartEvent.MidiNoteDragStart;
        int midiNoteDistance = (int)Mathf.Round(generalDragEvent.LocalCoordinateInPercent.Distance.y * noteAreaControl.ViewportHeight);

        int positionInMillisDragStart = dragStartEvent.PositionInMillisDragStart;
        int millisDistance = (int)Mathf.Round(generalDragEvent.LocalCoordinateInPercent.Distance.x * noteAreaControl.ViewportWidth);

        int positionInBeatsDragStart = dragStartEvent.PositionInBeatsDragStart;
        int beatDistance = (int)Mathf.Round(generalDragEvent.LocalCoordinateInPercent.Distance.x * noteAreaControl.ViewportWidthInBeats);

        NoteAreaDragEvent result = new(generalDragEvent,
            midiNoteDragStart, midiNoteDistance,
            positionInMillisDragStart, millisDistance,
            positionInBeatsDragStart, beatDistance);

        return result;
    }
}
