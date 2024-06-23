using System.Collections.Generic;
using NHyphenator;
using UniInject;

public class HyphenateNotesAction : INeedInjection
{
    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongEditorLayerManager layerManager;

    [Inject]
    private SongEditorSelectionControl selectionControl;
    
    [Inject]
    private Settings settings;

    [Inject]
    private SpaceBetweenNotesAction spaceBetweenNotesAction;
    
    public void Execute(SongMeta songMeta, List<Note> notes, Hyphenator hyphenator)
    {
        if (songMeta == null
            || notes.IsNullOrEmpty()
            || hyphenator == null)
        {
            return;
        }
        
        int spaceBetweenNotesInMillis = settings.SongEditorSettings.SpaceBetweenNotesInMillis;

        Dictionary<Note,List<Note>> noteToNotesAfterSplit = HyphenateNotesUtils.HypenateNotes(songMeta, notes, hyphenator);
        noteToNotesAfterSplit.ForEach(entry =>
        {
            Note note = entry.Key;
            List<Note> notesAfterSplit = entry.Value;
            if (note.Sentence != null)
            {
                notesAfterSplit.ForEach(noteAfterSplit => noteAfterSplit.SetSentence(note.Sentence));
            }
            else if (layerManager.TryGetLayerEnumOfNote(note, out ESongEditorLayer layerEnum))
            {
                notesAfterSplit.ForEach(noteAfterSplit => layerManager.AddNoteToEnumLayer(layerEnum, noteAfterSplit));
            }

            if (selectionControl.IsSelected(note))
            {
                selectionControl.AddToSelection(notesAfterSplit);
            }
            
            if (spaceBetweenNotesInMillis > 0)
            {
                spaceBetweenNotesAction.ExecuteAndNotify(songMeta, notesAfterSplit, spaceBetweenNotesInMillis);
            }
        });
    }

    public void ExecuteAndNotify(SongMeta songMeta, List<Note> selectedNotes, Hyphenator hyphenator)
    {
        Execute(songMeta, selectedNotes, hyphenator);
        songMetaChangeEventStream.OnNext(new NotesChangedEvent());
    }
}
