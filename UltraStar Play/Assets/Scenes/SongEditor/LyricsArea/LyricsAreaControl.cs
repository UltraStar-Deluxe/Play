using System.Collections.Generic;
using System.Text;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

#pragma warning disable CS0649

public class LyricsAreaControl : INeedInjection, IInjectionFinishedListener
{
    private static readonly char syllableSeparator = ';';
    private static readonly char sentenceSeparator = '\n';
    private static readonly char spaceCharacter = ' ';

    [Inject(UxmlName = R.UxmlNames.lyricsAreaTextField)]
    private TextField textField;

    [Inject(UxmlName = R.UxmlNames.lyricsArea)]
    private VisualElement lyricsArea;

    [Inject(UxmlName = R.UxmlNames.lyricsAreaVoice1Button)]
    private Button lyricsAreaVoice1Button;

    [Inject(UxmlName = R.UxmlNames.lyricsAreaVoice2Button)]
    private Button lyricsAreaVoice2Button;

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
        BackslashReplacingTextFieldControl backslashReplacingTextFieldControl = null;

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
                OnEndEdit(backslashReplacingTextFieldControl.UnescapeBackslashes(textField.text));
            }
        });

        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);

        textField.doubleClickSelectsWord = true;
        textField.tripleClickSelectsLine = true;

        backslashReplacingTextFieldControl = new BackslashReplacingTextFieldControl(textField);
        // Replace white space with visible characters when in edit mode
        backslashReplacingTextFieldControl.ValueChangedEventStream
            .Subscribe(newValue =>
            {
                if (lyricsAreaMode == LyricsAreaMode.EditMode)
                {
                    string normalText = ShowWhiteSpaceText.ReplaceVisibleCharactersWithWhiteSpace(newValue);
                    string visibleWhiteSpaceText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(normalText);
                    textField.SetValueWithoutNotify(visibleWhiteSpaceText);
                }
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
            SyncPositionInSongWithSelectedText();
        }
    }

    private void OnSongMetaChanged(SongMetaChangeEvent changeEvent)
    {
        if (lyricsAreaMode == LyricsAreaMode.ViewMode
            && (changeEvent is LyricsChangedEvent
                || changeEvent is LoadedMementoEvent
                || changeEvent is MovedNotesToVoiceEvent))
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
            ? GetViewModeText()
            : GetEditModeText();
        string newInputFieldText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(text);
        SetInputFieldText(newInputFieldText);
    }

    private void OnBeginEdit()
    {
        // Map lyrics of notes to edit-mode text.
        lastEditModeText = null;
        string editModeText = GetEditModeText();
        string newInputFieldText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(editModeText);
        SetInputFieldText(newInputFieldText);

        lyricsAreaMode = LyricsAreaMode.EditMode;
    }

    private void OnEndEdit(string newText)
    {
        ApplyEditModeText(newText, true);

        string newInputFieldText = ShowWhiteSpaceText.ReplaceWhiteSpaceWithVisibleCharacters(GetViewModeText());
        SetInputFieldText(newInputFieldText);

        lyricsAreaMode = LyricsAreaMode.ViewMode;
    }

    private void ApplyEditModeText(string editModeText, bool undoable)
    {
        // Map edit-mode text to lyrics of notes
        string text = ShowWhiteSpaceText.ReplaceVisibleCharactersWithWhiteSpace(editModeText);
        MapEditModeTextToNotes(text);
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
            float positionInSongInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat);
            songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
        }
    }

    private string GetEditModeText()
    {
        StringBuilder stringBuilder = new StringBuilder();
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(Voice);
        Note lastNote = null;

        void ProcessNote(Note note)
        {
            if (lastNote != null
                && lastNote.Sentence == note.Sentence)
            {
                // Add a space when the last note ended or the current note started with a space.
                // Otherwise use the non-whitespace syllabeSeparator as end-of-note.
                if (lastNote.Text.EndsWith(spaceCharacter)
                    || note.Text.StartsWith(spaceCharacter))
                {
                    stringBuilder.Append(spaceCharacter);
                }
                else
                {
                    stringBuilder.Append(syllableSeparator);
                }
            }
            stringBuilder.Append(note.Text.Trim());

            lastNote = note;
        }

        void ProcessSentence(Sentence sentence)
        {
            List<Note> sortedNotes = SongMetaUtils.GetSortedNotes(sentence);
            sortedNotes.ForEach(ProcessNote);

            stringBuilder.Append(sentenceSeparator);
        }

        sortedSentences.ForEach(ProcessSentence);

        return stringBuilder.ToString();
    }

    private string GetViewModeText()
    {
        StringBuilder stringBuilder = new StringBuilder();
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(Voice);
        foreach (Sentence sentence in sortedSentences)
        {
            List<Note> sortedNotes = SongMetaUtils.GetSortedNotes(sentence);
            foreach (Note note in sortedNotes)
            {
                stringBuilder.Append(note.Text);
            }
            stringBuilder.Append(sentenceSeparator);
        }
        return stringBuilder.ToString();
    }

    private void MapEditModeTextToNotes(string editModeText)
    {
        int sentenceIndex = 0;
        int noteIndex = 0;
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(Voice);
        List<Note> sortedNotes = (sentenceIndex < sortedSentences.Count)
            ? SongMetaUtils.GetSortedNotes(sortedSentences[sentenceIndex])
            : new List<Note>();

        StringBuilder stringBuilder = new StringBuilder();

        void ApplyNoteText()
        {
            if (noteIndex < sortedNotes.Count)
            {
                sortedNotes[noteIndex].SetText(stringBuilder.ToString());
            }
            stringBuilder = new StringBuilder();
        }

        void SelectNextSentence()
        {
            ApplyNoteText();

            for (int i = noteIndex + 1; i < sortedNotes.Count; i++)
            {
                sortedNotes[i].SetText("");
            }

            sentenceIndex++;
            noteIndex = 0;

            sortedNotes = (sentenceIndex < sortedSentences.Count)
                    ? SongMetaUtils.GetSortedNotes(sortedSentences[sentenceIndex])
                    : new List<Note>();
        }

        void SelectNextNote()
        {
            ApplyNoteText();

            noteIndex++;
        }

        foreach (char c in editModeText)
        {
            if (c == sentenceSeparator)
            {
                SelectNextSentence();
            }
            else if (c == syllableSeparator)
            {
                SelectNextNote();
            }
            else if (c == ' ')
            {
                stringBuilder.Append(c);
                SelectNextNote();
            }
            else
            {
                stringBuilder.Append(c);
            }
        }

        for (int s = sentenceIndex; s < sortedSentences.Count; s++)
        {
            sortedNotes = SongMetaUtils.GetSortedNotes(sortedSentences[s]);
            for (int n = noteIndex; n < sortedNotes.Count; n++)
            {
                sortedNotes[n].SetText("");
            }
        }
    }

    private Note GetNoteForCaretPosition(string text, int caretPosition)
    {
        // Count sentence borders
        int relevantSentenceIndex = 0;
        int relevantSentenceTextStartIndex = 0;
        for (int i = 0; i < text.Length && i < caretPosition; i++)
        {
            if (text[i] == sentenceSeparator)
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
            if (c == spaceCharacter
                || c == ShowWhiteSpaceText.spaceReplacement[0]
                || c == syllableSeparator)
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
