using System.Collections.Generic;
using UniInject;

#pragma warning disable CS0649

public class SpaceBetweenNotesAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private UiManager uiManager;

    public void Execute(IReadOnlyCollection<Note> selectedNotes, int spaceInBeats)
    {
        if (spaceInBeats <= 0)
        {
            UiManager.CreateNotification("Minimum amount of space (in beats) must be greater than 0.");
            return;
        }

        AddSpaceBetweenNotesUtils.AddSpaceInBeatsBetweenNotes(selectedNotes, spaceInBeats);
    }

    public void ExecuteAndNotify(IReadOnlyCollection<Note> selectedNotes, int spaceInBeats)
    {
        Execute(selectedNotes, spaceInBeats);
        songMetaChangeEventStream.OnNext(new NotesChangedEvent());
    }
}
