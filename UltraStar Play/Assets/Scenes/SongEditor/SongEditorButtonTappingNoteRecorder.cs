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

    [Inject(UxmlName = R.UxmlNames.buttonRecordingLyricsTextField)]
    private TextField buttonRecordingLyricsTextField;

    private List<Note> upcomingSortedRecordedNotes = new();

    private int lastPitchDetectedFrame;
    private int lastPitchDetectedBeat;
    private Note lastRecordedNote;

    private int cursorIndex;

    private bool hasRecordedNotes;

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
        if (Keyboard.current != null
            && Keyboard.current.anyKey.isPressed)
        {
            // Check if the required key is pressed
            List<string> pressedKeysDisplayNames = Keyboard.current.allControls
                .Where(inputControl => inputControl.IsPressed())
                .Select(inputControl => inputControl.displayName.ToUpperInvariant())
                .ToList();
            if (pressedKeysDisplayNames.Contains(settings.SongEditorSettings.ButtonDisplayNameForButtonRecording.ToUpperInvariant()))
            {
                RecordNote(settings.SongEditorSettings.DefaultPitchForCreatedNotes,
                    currentBeat,
                    ESongEditorLayer.ButtonRecording);
            }
        }
        else
        {
            lastRecordedNote = null;
            // The pitch is always detected (either the keyboard is down or not).
            lastPitchDetectedBeat = currentBeat;
        }
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
        int indexOfFirstSpaceOrNewline = StringUtils.MinIndexOf(remainingButtonTappingLyrics, 0, ' ', '\n');
        if (indexOfFirstSpaceOrNewline < 0)
        {
            cursorIndex = buttonRecordingLyricsTextField.text.Length;
            return remainingButtonTappingLyrics;
        }

        string firstWord = remainingButtonTappingLyrics.Substring(0, indexOfFirstSpaceOrNewline);
        cursorIndex += indexOfFirstSpaceOrNewline + 1;
        firstWord = firstWord.Replace("\n", "")
            .Replace(" ", "")
            .Replace(";", "");
        return firstWord;
    }

    private void ContinueLastRecordedNote(int currentBeat, ESongEditorLayer targetLayer)
    {
        if (currentBeat > lastRecordedNote.EndBeat)
        {
            lastRecordedNote.SetEndBeat(currentBeat);

            // EndBeat of extended note is currentBeat. Overwrite notes that start before this beat.
            OverwriteExistingNotes(currentBeat, targetLayer);
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
}
