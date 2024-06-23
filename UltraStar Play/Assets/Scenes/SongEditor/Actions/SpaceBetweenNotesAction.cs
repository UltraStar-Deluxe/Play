using System.Collections.Generic;
using UniInject;

#pragma warning disable CS0649

public class SpaceBetweenNotesAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private UiManager uiManager;

    public void Execute(SongMeta songMeta, IReadOnlyCollection<Note> selectedNotes, int spaceInMillis)
    {
        if (spaceInMillis <= 0)
        {
            NotificationManager.CreateNotification(Translation.Get(R.Messages.common_errorWithReason, "reason", "value too low"));
            return;
        }

        SpaceBetweenNotesUtils.AddSpaceInMillisBetweenNotes(selectedNotes, spaceInMillis, songMeta);
    }

    public void ExecuteAndNotify(SongMeta songMeta, IReadOnlyCollection<Note> selectedNotes, int spaceInMillis)
    {
        Execute(songMeta, selectedNotes, spaceInMillis);
        songMetaChangeEventStream.OnNext(new NotesChangedEvent());
    }
}
