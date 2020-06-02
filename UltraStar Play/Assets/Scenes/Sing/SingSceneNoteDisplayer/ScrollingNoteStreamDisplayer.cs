using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ScrollingNoteStreamDisplayer : AbstractSingSceneNoteDisplayer
{
    [InjectedInInspector]
    public RectTransform lyricsBar;

    public float pitchIndicatorAnchorX = 0.15f;
    public float lyricsHeightInPercent = 0.1f;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private Voice voice;

    private int micDelayInMillis;

    private List<Note> previousSentenceNotes = new List<Note>();

    void Update()
    {
        foreach (UiNote uiNote in noteToUiNoteMap.Values)
        {
            PositionUiNote(uiNote.RectTransform, uiNote.Note.MidiNote, uiNote.Note.StartBeat, uiNote.Note.EndBeat);
            PositionUiNoteLyricsInLyricsBar(uiNote);
        }

        foreach (UiRecordedNote uiRecordedNote in uiRecordedNotes)
        {
            // Draw the UiRecordedNotes smoothly from their StartBeat to TargetEndBeat
            if (uiRecordedNote.EndBeat < uiRecordedNote.TargetEndBeat)
            {
                UpdateUiRecordedNoteEndBeat(uiRecordedNote);
            }

            PositionUiNote(uiRecordedNote.RectTransform, uiRecordedNote.MidiNote, uiRecordedNote.StartBeat, uiRecordedNote.EndBeat);
        }
    }

    override public void Init(int lineCount)
    {
        if (!enabled || !gameObject.activeInHierarchy)
        {
            return;
        }

        if (micProfile != null)
        {
            micDelayInMillis = micProfile.DelayInMillis;
        }

        avgMidiNote = CalculateAvgMidiNote(voice.Sentences.SelectMany(sentence => sentence.Notes).ToList());
        base.Init(lineCount);
    }

    override public void DisplaySentence(Sentence sentence, Sentence nextSentence)
    {
        currentSentence = sentence;
        RemoveAllDisplayedNotes();
        if (sentence == null)
        {
            return;
        }

        // The division is rounded down on purpose (e.g. noteRowCount of 3 will result in (noteRowCount / 2) == 1)
        maxNoteRowMidiNote = avgMidiNote + (noteRowCount / 2);
        minNoteRowMidiNote = avgMidiNote - (noteRowCount / 2);
        // Freestyle notes are not drawn
        IEnumerable<Note> nonFreestyleNotes = sentence.Notes
            .Union(nextSentence.Notes)
            .Union(previousSentenceNotes)
            .Where(note => !note.IsFreestyle);
        foreach (Note note in nonFreestyleNotes)
        {
            CreateUiNote(note);
        }

        previousSentenceNotes = sentence != null
            ? sentence.Notes.ToList()
            : new List<Note>();
    }

    private int CalculateAvgMidiNote(IReadOnlyCollection<Note> notes)
    {
        return notes.Count > 0
            ? (int)notes.Select(it => it.MidiNote).Average()
            : 0;
    }

    override protected void PositionUiNote(RectTransform uiNote, int midiNote, double noteStartBeat, double noteEndBeat)
    {
        // Display the beats for the next 2 seconds.
        int displayedBeats = (int)Math.Ceiling(BpmUtils.GetBeatsPerSecond(songMeta)) * 2;

        // The VerticalPitchIndicator's position is the position where recording happens.
        // Thus, a note with startBeat == (currentBeat + micDelayInBeats) will have its left side drawn where the VerticalPitchIndicator is.
        double millisInSong = songAudioPlayer.PositionInSongInMillis - micDelayInMillis;
        double currentBeat = BpmUtils.MillisecondInSongToBeat(songMeta, millisInSong);

        Vector2 anchorY = GetAnchorYForMidiNote(midiNote);
        float anchorXStart = (float)((noteStartBeat - currentBeat) / displayedBeats) + pitchIndicatorAnchorX;
        float anchorXEnd = (float)((noteEndBeat - currentBeat) / displayedBeats) + pitchIndicatorAnchorX;

        uiNote.anchorMin = new Vector2(anchorXStart, anchorY.x);
        uiNote.anchorMax = new Vector2(anchorXEnd, anchorY.y);
        uiNote.MoveCornersToAnchors();
    }

    private void PositionUiNoteLyricsInLyricsBar(UiNote uiNote)
    {
        RectTransform lyricsUiNoteRectTransform = uiNote.lyricsUiText.GetComponent<RectTransform>();
        lyricsUiNoteRectTransform.SetParent(lyricsBar, true);
        lyricsUiNoteRectTransform.localPosition = new Vector2(lyricsUiNoteRectTransform.localPosition.x, 0);
        lyricsUiNoteRectTransform.sizeDelta = new Vector2(lyricsUiNoteRectTransform.sizeDelta.x, 0);
        uiNote.lyricsUiText.transform.SetParent(uiNote.RectTransform);
    }

    protected override UiNote CreateUiNote(Note note)
    {
        UiNote uiNote = base.CreateUiNote(note);
        if (uiNote != null)
        {
            uiNote.lyricsUiText.color = Color.white;
            uiNote.lyricsUiText.alignment = TextAnchor.MiddleLeft;
        }
        return uiNote;
    }

    protected override void RemoveUiRecordedNotes()
    {
        if (previousSentenceNotes.IsNullOrEmpty())
        {
            return;
        }

        // Only remove UiRecordedNotes if they are out of the screen.
        // This is the case when they end before the previous sentence begins.
        int previousSentenceEndStart = previousSentenceNotes.Select(note => note.StartBeat).Min();
        foreach (UiRecordedNote uiRecordedNote in new List<UiRecordedNote>(uiRecordedNotes))
        {
            if (uiRecordedNote.EndBeat < previousSentenceEndStart)
            {
                RemoveUiRecordedNote(uiRecordedNote);
            }
        }
    }

    private void RemoveUiRecordedNote(UiRecordedNote uiRecordedNote)
    {
        uiRecordedNotes.Remove(uiRecordedNote);
        recordedNoteToUiRecordedNotesMap.Remove(uiRecordedNote.RecordedNote);
        Destroy(uiRecordedNote.gameObject);
    }
}
