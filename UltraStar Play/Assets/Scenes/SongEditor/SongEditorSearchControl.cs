using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorSearchControl : INeedInjection, IInjectionFinishedListener
{
    [Inject]
    private SongAudioPlayer songAudioPlayer;
    
    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongEditorLayerManager layerManager;
    
    [Inject]
    private Injector injector;
    
    [Inject(UxmlName = R.UxmlNames.searchOverlay)]
    private VisualElement searchOverlay;
    
    [Inject(UxmlName = R.UxmlNames.searchContainer)]
    private VisualElement searchContainer;
    
    [Inject(UxmlName = R.UxmlNames.searchTextField)]
    private TextField searchTextField;
    
    [Inject(UxmlName = R.UxmlNames.searchPreviousButton)]
    private Button searchPreviousButton;
    
    [Inject(UxmlName = R.UxmlNames.searchNextButton)]
    private Button searchNextButton;
    
    [Inject(UxmlName = R.UxmlNames.searchResultLabel)]
    private Label searchResultLabel;

    public bool IsSearchOverlayVisible => searchOverlay.IsVisibleByDisplay();

    private DragToMoveControl dragToMoveControl;
    
    public void OnInjectionFinished()
    {
        HideSearchOverlay();
        UpdateSearchResultLabel();
        
        searchTextField.RegisterValueChangedCallback(evt => UpdateSearchResultLabel());
        searchTextField.RegisterCallback<NavigationSubmitEvent>(_ => SearchNext());
        
        searchPreviousButton.RegisterCallbackButtonTriggered(_ => SearchPrevious());
        searchNextButton.RegisterCallbackButtonTriggered(_ => SearchNext());

        VisualElementUtils.RegisterDirectClickCallback(searchOverlay, () => HideSearchOverlay());

        dragToMoveControl = injector
            .WithRootVisualElement(searchContainer)
            .CreateAndInject<DragToMoveControl>();
        dragToMoveControl.RequirePointerDirectlyOnTargetElement = true;
    }

    private void SearchPrevious()
    {
        SearchDirection(-1);
    }

    private void SearchNext()
    {
        SearchDirection(1);
    }

    private void SearchDirection(int direction)
    {
        string searchText = searchTextField.text;
        if (searchText.IsNullOrEmpty())
        {
            return;
        }

        // Find notes matching the search text
        List<Note> matchingNotes = SearchMatchingNotes();

        // Reduce search result to notes in direction
        int currentBeat = (int)songAudioPlayer.GetCurrentBeat(true);
        List<Note> matchingNotesInDirection = new();
        if (direction < 0)
        {
            matchingNotesInDirection = matchingNotes
                .Where(note => (note.EndBeat + 1) < currentBeat)
                .ToList();
        }
        else if (direction > 0)
        {
            matchingNotesInDirection = matchingNotes
                .Where(note => (note.StartBeat - 1) > currentBeat)
                .ToList();
        }
        
        // Find first note in direction
        Note note = null;
        matchingNotesInDirection.Sort(Note.comparerByStartBeat);
        if (direction < 0)
        {
            note = matchingNotesInDirection.LastOrDefault();
        }
        else if (direction > 0)
        {
            note = matchingNotesInDirection.FirstOrDefault();
        }
        
        // Wrap around
        if (note == null
            && !matchingNotes.IsNullOrEmpty())
        {
            List<Note> matchingNotesSorted = new(matchingNotes);
            matchingNotesSorted.Sort(Note.comparerByStartBeat);
            if (direction < 0)
            {
                note = matchingNotesSorted.LastOrDefault();
            }
            else if (direction > 0)
            {
                note = matchingNotesSorted.FirstOrDefault();
            }
        }
        
        if (note != null)
        {
            songAudioPlayer.PositionInSongInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat);
        }
    }

    private List<Note> SearchMatchingNotes()
    {
        string searchText = searchTextField.text;
        if (searchText.IsNullOrEmpty())
        {
            return new List<Note>();
        }
        
        HashSet<Note> matchingNotes = new();
        int searchStartIndex = 0;
        
        songMeta.GetVoices().ForEach(voice =>
        {
            try
            {
                if (!layerManager.IsVoiceLayerVisible(voice.Name))
                {
                    return;
                }
                
                List<Note> notesOfVoice = voice.Sentences
                    .SelectMany(sentence => sentence.Notes)
                    .ToList();
                NoteSearchData noteSearchData = new(notesOfVoice);
                if (noteSearchData.lyrics.IsNullOrEmpty())
                {
                    return;
                }
                
                int index;
                do
                {
                    index = noteSearchData.lyrics.IndexOf(searchText, searchStartIndex, StringComparison.InvariantCulture);
                    searchStartIndex = index + 1;

                    if (index >= 0
                        && noteSearchData.lyricsIndexToNote.ContainsKey(index))
                    {
                        Note note = noteSearchData.lyricsIndexToNote[index];
                        matchingNotes.Add(note);
                    }
                } while (index >= 0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                searchResultLabel.text = "Invalid search syntax";
            }
        });
        
        List<Note> sortedMatchingNotes = new(matchingNotes);
        sortedMatchingNotes.Sort(Note.comparerByStartBeat);
        return sortedMatchingNotes;
    }

    private void UpdateSearchResultLabel()
    {
        if (searchTextField.text.IsNullOrEmpty())
        {
            searchResultLabel.text = "";
            return;
        }

        List<Note> matchingNotes = SearchMatchingNotes();
        searchResultLabel.text = $"{matchingNotes.Count.ToString()} matches";
    }

    public void ShowSearchOverlay()
    {
        searchOverlay.ShowByDisplay();
        searchTextField.Focus();
    }
    
    public void HideSearchOverlay()
    {
        searchOverlay.HideByDisplay();
        // Move focus away from search text field
        searchTextField.Blur();
        MainThreadDispatcher.StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1,
            () => searchTextField.GetRootVisualElement().Q<Button>().Focus()));
    }

    private class NoteSearchData
    {
        public string lyrics;
        public Dictionary<int, Note> lyricsIndexToNote = new();

        public NoteSearchData(List<Note> notes)
        {
            StringBuilder sb = new();
            notes.ForEach(note =>
                {
                    if (note == null
                        || note.Text.IsNullOrEmpty())
                    {
                        return;
                    }

                    int oldLength = sb.Length;
                    string noteText = note.Text.ToLowerInvariant();
                    sb.Append(noteText);
                    int newLength = sb.Length;
                    
                    for (int i = oldLength; i < newLength; i++)
                    {
                        lyricsIndexToNote[i] = note;
                    }
                });
            lyrics = sb.ToString();
        }
    }
}
