using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System.Text;
using TMPro;

#pragma warning disable CS0649

public class LyricsArea : MonoBehaviour, INeedInjection
{
    private static readonly char syllableSeparator = ';';
    private static readonly char sentenceSeparator = '\n';
    private static readonly char spaceCharacter = ' ';

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private TMP_InputField inputField;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private ScrollRect scrollRect;

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
            UpdateLyrics();
        }
    }

    private int lastCaretPosition;

    private bool newlineAdded;

    private LyricsAreaMode lyricsAreaMode = LyricsAreaMode.ViewMode;

    private string lastEditModeText;

    void Start()
    {
        voice = songMeta.GetVoices()[0];
        UpdateLyrics();
        inputField.onSelect.AsObservable().Subscribe(_ => OnBeginEdit());
        inputField.onEndEdit.AsObservable().Subscribe(OnEndEdit);
        inputField.onValidateInput += OnValidateInput;
        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);
    }

    void Update()
    {
        if (lyricsAreaMode == LyricsAreaMode.EditMode)
        {
            // Make add visible character to the newline
            if (newlineAdded)
            {
                int caretPosition = inputField.caretPosition;
                string newInputFieldText = inputField.text
                    .Replace("\n", "")
                    .Replace("↵", ShowWhiteSpaceText.newlineReplacement);
                SetInputFieldText(newInputFieldText);
                inputField.caretPosition = caretPosition + 1;
                newlineAdded = false;
            }

            // Immediately apply changed lyrics to notes, but do not record it in the history.
            if (lastEditModeText != inputField.text)
            {
                if (lastEditModeText != null)
                {
                    ApplyEditModeText(inputField.text, false);
                }
                lastEditModeText = inputField.text;
            }
        }

        if (inputField.isFocused && lastCaretPosition != inputField.caretPosition)
        {
            lastCaretPosition = inputField.caretPosition;

            SyncPositionInSongWithSelectedText();
        }
    }

    private char OnValidateInput(string text, int charIndex, char addedChar)
    {
        if (addedChar == ' ')
        {
            return ShowWhiteSpaceText.spaceReplacement[0];
        }
        if (addedChar == '\n'
            && (charIndex == 0
                || text.Length < charIndex
                || text[charIndex - 1] != ShowWhiteSpaceText.newlineReplacement[0]))
        {
            newlineAdded = true;
            return ShowWhiteSpaceText.newlineReplacement[0];
        }
        return addedChar;
    }

    private void OnSongMetaChanged(SongMetaChangeEvent changeEvent)
    {
        if (lyricsAreaMode == LyricsAreaMode.ViewMode
            && (changeEvent is LyricsChangedEvent
                || changeEvent is LoadedMementoEvent))
        {
            UpdateLyrics();
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
        inputField.text = text;
        int lineBreaks = text.Select((char c) => c == '\n').Count();
        scrollRect.verticalScrollbar.numberOfSteps = lineBreaks;
    }

    private void SyncPositionInSongWithSelectedText()
    {
        Note note = GetNoteForCaretPosition(inputField.text, inputField.caretPosition);
        if (note != null)
        {
            double positionInSongInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat);
            songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
        }
    }

    private string GetEditModeText()
    {
        StringBuilder stringBuilder = new StringBuilder();
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(Voice);
        Note lastNote = null;
        foreach (Sentence sentence in sortedSentences)
        {
            List<Note> sortedNotes = SongMetaUtils.GetSortedNotes(sentence);
            foreach (Note note in sortedNotes)
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
            stringBuilder.Append(sentenceSeparator);
        }
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

        Action applyNoteText = () =>
        {
            if (noteIndex < sortedNotes.Count)
            {
                sortedNotes[noteIndex].SetText(stringBuilder.ToString());
            }
            stringBuilder = new StringBuilder();
        };

        Action selectNextSentence = () =>
        {
            applyNoteText();

            for (int i = noteIndex + 1; i < sortedNotes.Count; i++)
            {
                sortedNotes[i].SetText("");
            }

            sentenceIndex++;
            noteIndex = 0;

            sortedNotes = (sentenceIndex < sortedSentences.Count)
                    ? SongMetaUtils.GetSortedNotes(sortedSentences[sentenceIndex])
                    : new List<Note>();
        };

        Action selectNextNote = () =>
        {
            applyNoteText();

            noteIndex++;
        };

        foreach (char c in editModeText)
        {
            if (c == sentenceSeparator)
            {
                selectNextSentence();
            }
            else if (c == syllableSeparator)
            {
                selectNextNote();
            }
            else if (c == ' ')
            {
                stringBuilder.Append(c);
                selectNextNote();
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
        string relevantSentenceText = text.Substring(relevantSentenceTextStartIndex, caretPosition - relevantSentenceTextStartIndex);
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
}
