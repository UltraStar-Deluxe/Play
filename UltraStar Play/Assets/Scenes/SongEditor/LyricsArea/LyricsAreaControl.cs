using System.Collections.Generic;
using System.Text;
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
    private Button lyricsAreaVoice1Button;

    [Inject(UxmlName = R.UxmlNames.lyricsAreaVoice2Button)]
    private Button lyricsAreaVoice2Button;

    [Inject(UxmlName = R.UxmlNames.syncLyricsAreaToggle)]
    private Toggle syncLyricsAreaToggle;

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
        voice = songMeta.GetVoices()[0];
        UpdateLyrics();
        textField.RegisterCallback<FocusEvent>(evt =>
        {
            OnBeginEdit();
        });
        textField.RegisterCallback<BlurEvent>(evt =>
        {
            if (lyricsAreaMode == LyricsAreaMode.EditMode)
            {
                OnEndEdit(textField.text);
            }
        });

        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);

        textField.doubleClickSelectsWord = true;
        textField.tripleClickSelectsLine = true;

        // Check when a newline has been added. This requires adding an additional character to show white space.
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
        });

        // Replace white space with visible characters when in edit mode
        textField.RegisterValueChangedCallback(evt =>
        {
            if (lyricsAreaMode != LyricsAreaMode.EditMode)
            {
                return;
            }

            string newText = evt.newValue;
            if (addedNewline)
            {
                addedNewline = false;
                // Add whitespace character for newly added newline character
                newText = newText.Replace(ShowWhiteSpaceText.newlineReplacement, "⌇")
                    .Replace("\n", "⌇")
                    .Replace("⌇", ShowWhiteSpaceText.newlineReplacement);
            }
            else if (removeCharacter)
            {
                removeCharacter = false;
                // Remove whitespace character or newline character where the counterpart is missing
                newText = newText.Replace(ShowWhiteSpaceText.newlineReplacement, "⌇")
                    .Replace("\n", "")
                    .Replace(ShowWhiteSpaceText.newlineVisibleWhiteSpaceCharacter, "")
                    .Replace("⌇", ShowWhiteSpaceText.newlineReplacement);
            }
            string normalText = ShowWhiteSpaceText.ReplaceVisibleCharactersWithWhiteSpace(newText);
            string visibleWhiteSpaceText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(normalText);
            textField.SetValueWithoutNotify(visibleWhiteSpaceText);
        });

        lyricsAreaVoice1Button.RegisterCallbackButtonTriggered(() => Voice = songMeta.GetVoice(Voice.firstVoiceName));
        lyricsAreaVoice2Button.RegisterCallbackButtonTriggered(() => Voice = songMeta.GetVoice(Voice.secondVoiceName));

        UpdateVoiceButtons();
    }

    public void Update()
    {
        if (lyricsAreaMode == LyricsAreaMode.EditMode)
        {
            // Immediately apply changed lyrics to notes, but do not record it in the history.
            if (lastEditModeText != textField.text)
            {
                if (lastEditModeText != null)
                {
                    ApplyEditModeText(textField.text, false);
                }
                lastEditModeText = textField.text;
            }
        }

        if (textField.focusController.focusedElement == textField
            && lastCaretPosition != textField.cursorIndex)
        {
            lastCaretPosition = textField.cursorIndex;
            if (syncLyricsAreaToggle.value)
            {
                SyncPositionInSongWithSelectedText();
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
                or NotesSplitEvent)
        {
            UpdateLyrics();
        }

        if (changeEvent is MovedNotesToVoiceEvent)
        {
            UpdateVoiceButtons();
        }
    }

    private void UpdateVoiceButtons()
    {
        if (voice == null
            || songMeta.GetVoices().Count <= 1)
        {
           lyricsAreaVoice1Button.HideByDisplay();
           lyricsAreaVoice2Button.HideByDisplay();
        }
        else
        {
            lyricsAreaVoice1Button.ShowByDisplay();
            lyricsAreaVoice2Button.ShowByDisplay();

            if (voice.Name == Voice.soloVoiceName
                || voice.Name == Voice.firstVoiceName)
            {
                lyricsAreaVoice1Button.AddToClassList("selected");
                lyricsAreaVoice2Button.RemoveFromClassList("selected");
            }
            else if (voice.Name == Voice.secondVoiceName)
            {
                lyricsAreaVoice1Button.RemoveFromClassList("selected");
                lyricsAreaVoice2Button.AddToClassList("selected");
            }
        }
    }

    public void UpdateLyrics()
    {
        string text = (lyricsAreaMode == LyricsAreaMode.ViewMode)
            ? LyricsUtils.GetViewModeText(Voice)
            : LyricsUtils.GetEditModeText(Voice);
        string newInputFieldText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(text);
        SetInputFieldText(newInputFieldText);
    }

    private void OnBeginEdit()
    {
        // Map lyrics of notes to edit-mode text.
        lastEditModeText = null;
        string editModeText = LyricsUtils.GetEditModeText(Voice);
        string newInputFieldText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(editModeText);
        SetInputFieldText(newInputFieldText);

        lyricsAreaMode = LyricsAreaMode.EditMode;
    }

    private void OnEndEdit(string newText)
    {
        ApplyEditModeText(newText, true);

        string newInputFieldText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(LyricsUtils.GetViewModeText(Voice));
        SetInputFieldText(newInputFieldText);

        lyricsAreaMode = LyricsAreaMode.ViewMode;
    }

    private void ApplyEditModeText(string editModeText, bool undoable)
    {
        // Map edit-mode text to lyrics of notes
        string text = ShowWhiteSpaceText.ReplaceVisibleCharactersWithWhiteSpace(editModeText);
        LyricsUtils.MapEditModeTextToNotes(text, Voice.Sentences);
        songMetaChangeEventStream.OnNext(new LyricsChangedEvent { Undoable = undoable });
    }

    private void SetInputFieldText(string text)
    {
        textField.value = text;
    }

    private void SyncPositionInSongWithSelectedText()
    {
        Note note = GetNoteForCaretPosition(textField.text, textField.cursorIndex);
        if (note != null)
        {
            double positionInSongInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat);
            songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
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
                || c == ShowWhiteSpaceText.spaceReplacement[0]
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

    public bool InputFieldHasFocus()
    {
        return textField.focusController.focusedElement == textField;
    }
}
