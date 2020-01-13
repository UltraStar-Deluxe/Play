using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System.Text;

#pragma warning disable CS0649

public class LyricsArea : MonoBehaviour, INeedInjection
{
    [Inject(searchMethod = SearchMethods.GetComponentInChildren)]
    private InputField inputField;

    [Inject]
    private SongMeta songMeta;

    [Inject]
    private SongMetaChangeEventStream songMetaChangeEventStream;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    private readonly Dictionary<int, Note> positionInLyricsToNoteMap = new Dictionary<int, Note>();

    private int lastCaretPosition;

    private string lyrics;

    void Start()
    {
        UpdateLyrics();
        inputField.OnEndEditAsObservable().Subscribe(OnEndEdit);
        songMetaChangeEventStream.Subscribe(OnSongMetaChanged);
    }

    void Update()
    {
        if (inputField.isFocused && lastCaretPosition != inputField.caretPosition)
        {
            lastCaretPosition = inputField.caretPosition;

            SyncPositionInSongWithSelectedText();
        }
    }

    private void OnSongMetaChanged(ISongMetaChangeEvent changeEvent)
    {
        if (changeEvent is LyricsChangedEvent)
        {
            UpdateLyrics();
        }
    }

    public void UpdateLyrics()
    {
        lyrics = GetLyrics();
        inputField.text = lyrics;
    }

    private void OnEndEdit(string newText)
    {
        // TODO: Change the lyrics if only the lyrics for a single note changed
        // Ignore new lyrics for now.
        inputField.text = lyrics;
    }

    private void SyncPositionInSongWithSelectedText()
    {
        Note note = GetNoteForPositionInLyrics(inputField.caretPosition);
        if (note != null)
        {
            double positionInSongInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, note.StartBeat);
            songAudioPlayer.PositionInSongInMillis = positionInSongInMillis;
        }
    }

    public string GetLyrics()
    {
        positionInLyricsToNoteMap.Clear();

        StringBuilder stringBuilder = new StringBuilder();
        List<Sentence> sortedSentences = SongMetaUtils.GetSortedSentences(songMeta);
        Note lastNote = null;
        foreach (Sentence sentence in sortedSentences)
        {
            List<Note> sortedNotes = SongMetaUtils.GetSortedNotes(sentence);
            foreach (Note note in sortedNotes)
            {
                foreach (char c in note.Text)
                {
                    positionInLyricsToNoteMap[stringBuilder.Length] = note;
                    stringBuilder.Append(c);
                }
                lastNote = note;
            }
            stringBuilder.Append("\n");
        }
        if (lastNote != null)
        {
            positionInLyricsToNoteMap[stringBuilder.Length] = lastNote;
        }
        return stringBuilder.ToString();
    }

    public Note GetNoteForPositionInLyrics(int positionInLyrics)
    {
        if (positionInLyricsToNoteMap.TryGetValue(positionInLyrics, out Note note))
        {
            return note;
        }
        else
        {
            return null;
        }
    }
}
