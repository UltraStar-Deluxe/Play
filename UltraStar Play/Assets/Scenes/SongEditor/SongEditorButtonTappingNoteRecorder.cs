using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SongEditorButtonTappingNoteRecorder : MonoBehaviour, INeedInjection
{
    [Inject]
    private Settings settings;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SongEditorLayerManager songEditorLayerManager;

    [Inject]
    private EditorNoteDisplayer editorNoteDisplayer;

    [Inject]
    private SongEditorHistoryManager historyManager;

    [Inject]
    private SongEditorSelectionControl songEditorSelectionControl;

    [Inject(UxmlName = R.UxmlNames.buttonRecordingLyricsTextField)]
    private TextField buttonRecordingLyricsTextField;

    private List<Note> upcomingSortedRecordedNotes = new();

    private int lastPitchDetectedFrame;
    private int lastPitchDetectedBeat;
    private Note lastRecordedNote;

    private int cursorIndex;

    private bool hasRecordedNotes;

    // State for editing an already selected existing note
    private bool isEditingSelectedNote;
    private List<Note> sortedNotesInEditScope = new();
    private int indexOfEditingNoteInSorted = -1;
    private bool lastFrameButtonPressed;

    private void Start()
    {
        songAudioPlayer.JumpBackEventStream.Subscribe(OnJumpedBackInSong);
        songAudioPlayer.PlaybackStartedEventStream.Subscribe(OnPlaybackStarted);
        songAudioPlayer.PlaybackStoppedEventStream.Subscribe(OnPlaybackStopped);

        buttonRecordingLyricsTextField.RegisterCallback<BlurEvent>(evt =>
        {
            // After pasting new text, move the cursor to the start
            if (buttonRecordingLyricsTextField.cursorIndex >= buttonRecordingLyricsTextField.text.Length)
            {
                buttonRecordingLyricsTextField.cursorIndex = 0;
                cursorIndex = 0;
            }
        });
    }

    private void OnPlaybackStopped(double positionInMillis)
    {
        if (hasRecordedNotes)
        {
            historyManager.AddUndoState();
        }
    }

    private void OnPlaybackStarted(double positionInMillis)
    {
        hasRecordedNotes = false;
        lastPitchDetectedBeat = GetBeat(positionInMillis);
        upcomingSortedRecordedNotes = GetUpcomingSortedRecordedNotes();
    }

    void Update()
    {
        if (songAudioPlayer.IsPlaying)
        {
            UpdateRecordingViaButtonClick();
        }

        // Synchronize cursorIndex with TextField
        bool isTextFieldFocused = buttonRecordingLyricsTextField.focusController?.focusedElement == buttonRecordingLyricsTextField;
        if (isTextFieldFocused)
        {
            cursorIndex = buttonRecordingLyricsTextField.cursorIndex;
        }
        else if (cursorIndex != buttonRecordingLyricsTextField.cursorIndex)
        {
            buttonRecordingLyricsTextField.cursorIndex = cursorIndex;
        }
    }

    private void OnJumpedBackInSong(Pair<double> previousAndNewPositionInMillis)
    {
        int currentBeat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, previousAndNewPositionInMillis.Current);
        lastPitchDetectedBeat = currentBeat;
        upcomingSortedRecordedNotes = GetUpcomingSortedRecordedNotes();
    }

    private void UpdateRecordingViaButtonClick()
    {
        int currentBeat = (int)songAudioPlayer.GetCurrentBeat(true);

        bool buttonPressed = IsRecordingButtonPressed();
        bool buttonDown = buttonPressed && !lastFrameButtonPressed;
        bool buttonUp = !buttonPressed && lastFrameButtonPressed;

        if (buttonDown)
        {
            List<Note> selectedNotes = songEditorSelectionControl.GetSelectedNotes();
            if (selectedNotes.Count == 1)
            {
                // Enter edit-existing-note mode
                Note selected = selectedNotes[0];
                StartEditSelectedNote(selected, currentBeat);
            }
        }

        if (buttonPressed)
        {
            if (isEditingSelectedNote)
            {
                // Extend the selected note while holding
                ContinueEditingSelectedNote(currentBeat);
            }
            else
            {
                // Create/extend notes in ButtonRecording layer
                RecordNote(settings.SongEditorSettings.DefaultPitchForCreatedNotes,
                    currentBeat,
                    ESongEditorLayer.ButtonRecording);
            }
        }
        else
        {
            if (buttonUp && isEditingSelectedNote)
            {
                // On release, select next note
                SelectNextNoteAfterEditing();
                isEditingSelectedNote = false;
            }

            lastRecordedNote = null;
            // The pitch is always detected (either the keyboard is down or not).
            lastPitchDetectedBeat = currentBeat;
        }

        lastFrameButtonPressed = buttonPressed;
    }

    private void RecordNote(int midiNote, int beat, ESongEditorLayer targetLayer)
    {
        if (beat <= lastPitchDetectedBeat)
        {
            return;
        }

        if (lastRecordedNote != null
            && lastRecordedNote.MidiNote == midiNote)
        {
            ContinueLastRecordedNote(beat, targetLayer);
        }
        else
        {
            CreateNewRecordedNote(midiNote, beat, targetLayer);
        }

        editorNoteDisplayer.UpdateNotes();

        lastPitchDetectedFrame = Time.frameCount;
        lastPitchDetectedBeat = beat;
        hasRecordedNotes = true;
    }

    private void CreateNewRecordedNote(int midiNote, int currentBeat, ESongEditorLayer targetLayer)
    {
        string text = GetCurrentWordForButtonTappingAndSelectNextWord();
        Note newNote = new Note(ENoteType.Normal, currentBeat, 1, midiNote - 60, text);
        songEditorLayerManager.AddNoteToEnumLayer(targetLayer, newNote);
        lastRecordedNote = newNote;

        // EndBeat of new note is currentBeat + 1. Overwrite notes that start before this beat.
        OverwriteExistingNotes(currentBeat + 1, targetLayer);
    }

    private string GetCurrentWordForButtonTappingAndSelectNextWord()
    {
        string buttonTappingLyrics = settings.SongEditorSettings.ButtonRecordingLyrics;
        if (buttonTappingLyrics.IsNullOrEmpty())
        {
            return "";
        }

        if (cursorIndex < 0
            || cursorIndex >= buttonTappingLyrics.Length)
        {
            return "";
        }

        string remainingButtonTappingLyrics = buttonTappingLyrics.Substring(cursorIndex);
        int indexOfSeparator = StringUtils.MinIndexOf(remainingButtonTappingLyrics, 0, ' ', ';', '\n');
        if (indexOfSeparator < 0)
        {
            cursorIndex = buttonRecordingLyricsTextField.text.Length;
            return remainingButtonTappingLyrics;
        }

        string word = remainingButtonTappingLyrics.Substring(0, indexOfSeparator + 1);
        cursorIndex += indexOfSeparator + 1;
        if (word.EndsWith('\n'))
        {
            word = word.Replace("\n", "");
        }
        else if(word.EndsWith(';'))
        {
            word = word.Replace(";", "");

        }
        return word;
    }

    private void ContinueLastRecordedNote(int currentBeat, ESongEditorLayer targetLayer)
    {
        if (currentBeat > lastRecordedNote.EndBeat)
        {
            lastRecordedNote.SetEndBeat(currentBeat);

            // EndBeat of extended note is currentBeat. Overwrite notes that start before this beat.
            // Do not overwrite existing notes when editing an already selected existing note.
            if (!isEditingSelectedNote)
            {
                OverwriteExistingNotes(currentBeat, targetLayer);
            }
        }
    }

    private void OverwriteExistingNotes(int currentBeat, ESongEditorLayer targetLayer)
    {
        // Move the start beat of existing notes behind the given beat.
        // If afterwards no length would be left (or negative), then remove the note completely.
        List<Note> overlappingNotes = new();
        int behindNoteCount = 0;
        foreach (Note upcomingNote in upcomingSortedRecordedNotes)
        {
            // Do not shorten the note that is currently beeing recorded.
            if (upcomingNote == lastRecordedNote)
            {
                continue;
            }

            if (upcomingNote.StartBeat < currentBeat && currentBeat <= upcomingNote.EndBeat)
            {
                overlappingNotes.Add(upcomingNote);
            }
            else if (upcomingNote.EndBeat < currentBeat)
            {
                // The position is behind the note, thus this note is not 'upcoming' anymore.
                behindNoteCount++;
            }
            else if (upcomingNote.EndBeat > currentBeat)
            {
                // The list is sorted, thus the other notes in the list will also not overlap with the currentBeat.
                break;
            }
        }
        if (behindNoteCount > 0)
        {
            upcomingSortedRecordedNotes.RemoveRange(0, behindNoteCount);
        }

        foreach (Note note in overlappingNotes)
        {
            if (note.EndBeat > currentBeat)
            {
                note.SetStartBeat(currentBeat);
            }
            else
            {
                songEditorLayerManager.RemoveNoteFromAllEnumLayers(note);
                editorNoteDisplayer.RemoveNoteControl(note);
            }
        }
    }

    private List<Note> GetUpcomingSortedRecordedNotes()
    {
        int currentBeat = GetBeat(songAudioPlayer.PositionInMillis - settings.SongEditorSettings.MicDelayInMillis);
        ESongEditorLayer targetLayer = GetRecordingTargetLayer();
        List<Note> result = songEditorLayerManager.GetEnumLayerNotes(targetLayer).Where(note => (note.StartBeat >= currentBeat)).ToList();
        result.Sort(Note.comparerByStartBeat);
        return result;
    }

    private ESongEditorLayer GetRecordingTargetLayer()
    {
        return ESongEditorLayer.ButtonRecording;
    }

    private int GetBeat(double positionInMillis)
    {
        int beat = (int)SongMetaBpmUtils.MillisToBeats(songMeta, positionInMillis);
        return beat;
    }

    private bool IsRecordingButtonPressed()
    {
        if (Keyboard.current == null || !Keyboard.current.anyKey.isPressed)
        {
            return false;
        }

        string target = settings.SongEditorSettings.ButtonDisplayNameForButtonRecording.ToUpperInvariant();
        List<string> pressedKeysDisplayNames = Keyboard.current.allControls
            .Where(inputControl => inputControl.IsPressed())
            .Select(inputControl => inputControl.displayName.ToUpperInvariant())
            .ToList();
        return pressedKeysDisplayNames.Contains(target);
    }

    private void StartEditSelectedNote(Note selected, int currentBeat)
    {
        // Compute sorted list of notes in same scope (voice or enum layer) from original order at button down
        sortedNotesInEditScope = GetNotesInSameScope(selected);
        sortedNotesInEditScope.Sort(Note.comparerByStartBeat);
        indexOfEditingNoteInSorted = sortedNotesInEditScope.IndexOf(selected);

        // Move selected note to current playback position and set minimal length
        lastRecordedNote = selected;
        if (currentBeat <= lastPitchDetectedBeat)
        {
            // Ensure we only move forward similar to creation logic; still place at least at lastPitchDetectedBeat+1
            currentBeat = lastPitchDetectedBeat + 1;
        }
        selected.SetStartBeat(currentBeat);
        selected.SetEndBeat(currentBeat + 1);
        editorNoteDisplayer.UpdateNotes();
        hasRecordedNotes = true;
        isEditingSelectedNote = true;
        lastPitchDetectedBeat = currentBeat;
        lastPitchDetectedFrame = Time.frameCount;
    }

    private void ContinueEditingSelectedNote(int currentBeat)
    {
        if (lastRecordedNote == null)
        {
            return;
        }
        if (currentBeat <= lastPitchDetectedBeat)
        {
            return;
        }

        if (currentBeat > lastRecordedNote.EndBeat)
        {
            lastRecordedNote.SetEndBeat(currentBeat);
            editorNoteDisplayer.UpdateNotes();
            lastPitchDetectedBeat = currentBeat;
            lastPitchDetectedFrame = Time.frameCount;
        }
    }

    private void SelectNextNoteAfterEditing()
    {
        if (sortedNotesInEditScope.IsNullOrEmpty() || indexOfEditingNoteInSorted < 0)
        {
            return;
        }
        int nextIndex = indexOfEditingNoteInSorted + 1;
        if (nextIndex >= 0 && nextIndex < sortedNotesInEditScope.Count)
        {
            Note next = sortedNotesInEditScope[nextIndex];
            songEditorSelectionControl.SetSelection(new List<Note> { next });
        }
    }

    private List<Note> GetNotesInSameScope(Note reference)
    {
        // Same voice if note belongs to a voice; otherwise same enum layer
        if (reference.Sentence?.Voice != null)
        {
            return songEditorLayerManager.GetVoiceLayerNotes(reference.Sentence.Voice.Id);
        }

        if (songEditorLayerManager.TryGetEnumLayer(reference, out SongEditorEnumLayer enumLayer))
        {
            return songEditorLayerManager.GetEnumLayerNotes(enumLayer.LayerEnum);
        }

        // Fallback: empty list
        return new List<Note>();
    }
}
