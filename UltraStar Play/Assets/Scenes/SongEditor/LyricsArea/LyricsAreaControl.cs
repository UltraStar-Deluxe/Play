using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class LyricsAreaControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(UxmlName = R.UxmlNames.lyricsAreaTextField)]
    private TextField textField;

    [Inject(UxmlName = R.UxmlNames.lyricsArea)]
    private VisualElement lyricsArea;

    [Inject(UxmlName = R.UxmlNames.lyricsAreaVoice1Button)]
    private ToggleButton lyricsAreaVoice1Button;

    [Inject(UxmlName = R.UxmlNames.lyricsAreaVoice2Button)]
    private ToggleButton lyricsAreaVoice2Button;

    [Inject(UxmlName = R.UxmlNames.syncLyricsAreaToggle)]
    private Toggle syncLyricsAreaToggle;

    [Inject(UxmlName = R.UxmlNames.toggleLyricsAreaEditModeButton)]
    private ToggleButton toggleLyricsAreaEditModeButton;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private Injector injector;

    private Voice voice;
    public Voice Voice
    {
        get
        {
            return voice;
        }
        set
        {
            voice = value;
            UpdateVoiceButtons();
            UpdateLyrics();
        }
    }

    private int lastCaretPosition;

    private LyricsAreaMode lyricsAreaMode = LyricsAreaMode.ViewMode;

    private string lastEditModeText;

    public void OnInjectionFinished()
    {
        voice = songMeta.Voices.FirstOrDefault();
        EnterViewMode();

        textField.DisableParseEscapeSequences();
        textField.selectAllOnMouseUp = false;
        textField.selectAllOnFocus = false;
        textField.RegisterCallback<BlurEvent>(evt =>
        {
            if (lyricsAreaMode == LyricsAreaMode.EditMode)
            {
                ApplyEditModeText(textField.value, true);
            }
        });
        toggleLyricsAreaEditModeButton.RegisterCallbackButtonTriggered(_ => ToggleEditMode());

        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);

        textField.doubleClickSelectsWord = true;
        textField.tripleClickSelectsLine = true;

        // Check when a newline has been added. This requires adding an additional character to show white space.
        bool pasted = false;
        bool addedNewline = false;
        bool removeCharacter = false;
        textField.RegisterCallback<KeyDownEvent>(evt =>
        {
            if (evt.keyCode == KeyCode.Return)
            {
                addedNewline = true;
            }
            else if (evt.keyCode == KeyCode.Delete
                    || evt.keyCode == KeyCode.Backspace)
            {
                removeCharacter = true;
            }
            else if (evt.keyCode == KeyCode.V
                     && evt.ctrlKey
                     && !evt.shiftKey
                     && !evt.altKey)
            {
                pasted = true;
            }
        });

        // Replace white space with visible characters when in edit mode
        textField.RegisterValueChangedCallback(evt =>
        {
            if (lyricsAreaMode != LyricsAreaMode.EditMode)
            {
                return;
            }

            string newText = evt.newValue;
            if (addedNewline
                || pasted)
            {
                addedNewline = false;
                pasted = false;
                // Add whitespace character for newly added newline character
                newText = newText.Replace(ShowWhiteSpaceUtils.newlineReplacement, "⌇")
                    .Replace("\n", "⌇")
                    .Replace("⌇", ShowWhiteSpaceUtils.newlineReplacement);
            }
            else if (removeCharacter)
            {
                removeCharacter = false;
                // Remove whitespace character or newline character where the counterpart is missing
                newText = newText.Replace(ShowWhiteSpaceUtils.newlineReplacement, "⌇")
                    .Replace("\n", "")
                    .Replace(ShowWhiteSpaceUtils.newlineVisibleWhiteSpaceCharacter, "")
                    .Replace("⌇", ShowWhiteSpaceUtils.newlineReplacement);
            }
            string normalText = ShowWhiteSpaceUtils.ReplaceVisibleCharactersWithWhiteSpace(newText);
            string visibleWhiteSpaceText = ShowWhiteSpaceUtils.ReplaceWhiteSpaceWithVisibleCharacters(normalText);
            textField.SetValueWithoutNotify(visibleWhiteSpaceText);
        });

        lyricsAreaVoice1Button.RegisterCallbackButtonTriggered(_ => TrySetVoice(EVoiceId.P1));
        lyricsAreaVoice2Button.RegisterCallbackButtonTriggered(_ => TrySetVoice(EVoiceId.P2));
        UpdateVoiceButtons();
    }

    private void TrySetVoice(EVoiceId voiceId)
    {
        Voice newVoice = SongMetaUtils.GetVoiceById(songMeta, voiceId);
        if (newVoice == null)
        {
            return;
        }
        Voice = newVoice;
    }

    private void ToggleEditMode()
    {
        LyricsAreaMode newLyricsAreaMode = lyricsAreaMode == LyricsAreaMode.EditMode
            ? LyricsAreaMode.ViewMode
            : LyricsAreaMode.EditMode;

        toggleLyricsAreaEditModeButton.SetActive(newLyricsAreaMode == LyricsAreaMode.EditMode);

        if (newLyricsAreaMode == LyricsAreaMode.EditMode)
        {
            EnterEditMode();
        }
        else
        {
            EnterViewMode();
        }
    }

    public void Update()
    {
        if (lyricsAreaMode == LyricsAreaMode.EditMode)
        {
            // Immediately apply changed lyrics to notes, but do not record it in the history.
            if (lastEditModeText != textField.value)
            {
                if (lastEditModeText != null)
                {
                    ApplyEditModeText(textField.value, false);
                }
                lastEditModeText = textField.value;
            }
        }

        if (textField.focusController.focusedElement == textField
            && lastCaretPosition != textField.cursorIndex)
        {
            lastCaretPosition = textField.cursorIndex;
            if (syncLyricsAreaToggle.value)
            {
                SyncPositionWithSelectedText();
            }
        }
    }

    private void OnSongMetaChanged(SongMetaChangeEvent changeEvent)
    {
        if (lyricsAreaMode == LyricsAreaMode.ViewMode
            && changeEvent
                is LyricsChangedEvent
                or LoadedMementoEvent
                or MovedNotesToVoiceEvent
                or NotesSplitEvent
                or NotesDeletedEvent
                or SentencesDeletedEvent
                or NotesPastedEvent
                or NotesAddedEvent
                or SentencesChangedEvent
                or NotesChangedEvent)
        {
            UpdateLyrics();
        }

        if (changeEvent
            is MovedNotesToVoiceEvent
            or NotesAddedEvent)
        {
            UpdateVoiceButtons();
        }
    }

    private void UpdateVoiceButtons()
    {
        lyricsAreaVoice1Button.SetEnabled(songMeta.VoiceCount >= 1);
        lyricsAreaVoice2Button.SetEnabled(songMeta.VoiceCount >= 2);
        lyricsAreaVoice1Button.SetActive(voice == SongMetaUtils.GetVoiceById(songMeta, EVoiceId.P1));
        lyricsAreaVoice2Button.SetActive(voice == SongMetaUtils.GetVoiceById(songMeta, EVoiceId.P2));
    }

    public void UpdateLyrics()
    {
        string text = lyricsAreaMode == LyricsAreaMode.ViewMode
            ? LyricsUtils.GetViewModeText(Voice)
            : LyricsUtils.GetEditModeText(Voice);
        SetInputFieldText(text);
    }

    private void EnterEditMode()
    {
        lastEditModeText = null;
        string editModeText = LyricsUtils.GetEditModeText(Voice);
        string newInputFieldText = ShowWhiteSpaceUtils.ReplaceWhiteSpaceWithVisibleCharacters(editModeText);
        SetInputFieldText(newInputFieldText);

        lyricsAreaMode = LyricsAreaMode.EditMode;
        textField.isReadOnly = false;
    }

    private void EnterViewMode()
    {
        string viewModeText = LyricsUtils.GetViewModeText(Voice);
        SetInputFieldText(viewModeText);

        lyricsAreaMode = LyricsAreaMode.ViewMode;
        textField.isReadOnly = true;
    }

    private void ApplyEditModeText(string editModeText, bool undoable)
    {
        // Map edit-mode text to lyrics of notes
        string text = ShowWhiteSpaceUtils.ReplaceVisibleCharactersWithWhiteSpace(editModeText);
        LyricsUtils.MapEditModeTextToNotes(text, Voice.Sentences);
        songMetaChangeEventStream.OnNext(new LyricsChangedEvent { Undoable = undoable });
    }

    private void SetInputFieldText(string text)
    {
        bool wasReadOnly = textField.isReadOnly;
        textField.isReadOnly = false;
        textField.value = text;
        textField.isReadOnly = wasReadOnly;
    }

    private void SyncPositionWithSelectedText()
    {
        Note note = GetNoteForCaretPosition(textField.value, textField.cursorIndex);
        if (note != null)
        {
            double positionInMillis = SongMetaBpmUtils.BeatsToMillis(songMeta, note.StartBeat);
            songAudioPlayer.PositionInMillis = positionInMillis;
        }
    }

    private Note GetNoteForCaretPosition(string text, int caretPosition)
    {
        // Count sentence borders
        int relevantSentenceIndex = 0;
        int relevantSentenceTextStartIndex = 0;
        for (int i = 0; i < text.Length && i < caretPosition; i++)
        {
            if (text[i] == LyricsUtils.sentenceSeparator)
            {
                relevantSentenceIndex++;
                relevantSentenceTextStartIndex = i + 1;
            }
        }

        // Get relevant sentence
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(Voice);
        if (relevantSentenceIndex >= sortedSentences.Count
            || sortedSentences[relevantSentenceIndex].Notes.IsNullOrEmpty())
        {
            return null;
        }
        Sentence relevantSentence = sortedSentences[relevantSentenceIndex];

        // Count note borders
        int noteIndex = 0;
        for (int i = relevantSentenceTextStartIndex; i < text.Length && i < caretPosition; i++)
        {
            char c = text[i];
            if (c == LyricsUtils.spaceCharacter
                || c == ShowWhiteSpaceUtils.spaceReplacement[0]
                || c == LyricsUtils.syllableSeparator)
            {
                noteIndex++;
            }
        }

        // Get relevant note
        List<Note> sortedNotes = SongMetaUtils.GetSortedNotes(relevantSentence);
        if (noteIndex >= sortedNotes.Count)
        {
            return null;
        }
        return sortedNotes[noteIndex];
    }
}
