using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using UniInject;
using UnityEngine;
using UnityEngine.UI;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SentenceDisplayer : MonoBehaviour, INeedInjection
{
    // The number of lines on which notes can be placed.
    // One can imagine that notes can be placed not only on the drawn lines,
    // but also the rows between two lines.
    // 
    // This must be a multiply of 12, such that a note that is shifted by an octave
    // will be wrapped around and placed on the same line as without the shift
    // (so only relative note value is relevant).
    private int noteLineCount = 12;

    public UiNote uiNotePrefab;
    public UiRecordedNote uiRecordedNotePrefab;

    public StarParticle perfectSentenceStarPrefab;

    public RectTransform uiNotesContainer;
    public RectTransform uiRecordedNotesContainer;
    public RectTransform uiEffectsContainer;

    public bool displayRoundedAndActualRecordedNotes;
    public bool showPitchOfNotes;

    [Inject]
    private Settings settings;

    private readonly Dictionary<RecordedNote, List<UiRecordedNote>> recordedNoteToUiRecordedNotesMap = new Dictionary<RecordedNote, List<UiRecordedNote>>();
    private readonly Dictionary<Note, UiNote> noteToUiNoteMap = new Dictionary<Note, UiNote>();

    private Sentence displayedSentence;

    private MicProfile micProfile;

    private int avgMidiNote;

    public void Init(int noteLineCount, MicProfile micProfile)
    {
        this.micProfile = micProfile;
        this.noteLineCount = noteLineCount;
    }

    public void RemoveAllDisplayedNotes()
    {
        RemoveUiNotes();
        RemoveUiRecordedNotes();
    }

    public void DisplaySentence(Sentence sentence)
    {
        displayedSentence = sentence;
        RemoveAllDisplayedNotes();
        if (sentence == null)
        {
            return;
        }

        avgMidiNote = (int)displayedSentence.Notes.Select(it => it.MidiNote).Average();
        foreach (Note note in sentence.Notes)
        {
            CreateUiNote(note);
        }
    }

    public void DisplayRecordedNote(RecordedNote recordedNote)
    {
        // Try to update existing recorded notes.
        if (recordedNoteToUiRecordedNotesMap.TryGetValue(recordedNote, out List<UiRecordedNote> uiRecordedNotes))
        {
            foreach (UiRecordedNote uiRecordedNote in uiRecordedNotes)
            {
                PositionUiNote(uiRecordedNote.RectTransform, uiRecordedNote.MidiNote, recordedNote.StartBeat, recordedNote.EndBeat);
            }
            return;
        }

        // Draw the bar for the rounded note
        // and draw the bar for the actually recorded pitch if needed.
        CreateUiRecordedNote(recordedNote, true);
        if (displayRoundedAndActualRecordedNotes && (recordedNote.RecordedMidiNote != recordedNote.RoundedMidiNote))
        {
            CreateUiRecordedNote(recordedNote, false);
        }
    }

    public void CreatePerfectNoteEffect(Note perfectNote)
    {
        if (noteToUiNoteMap.TryGetValue(perfectNote, out UiNote uiNote))
        {
            uiNote.CreatePerfectNoteEffect();
        }
    }

    private void RemoveUiNotes()
    {
        foreach (Transform child in uiNotesContainer.transform)
        {
            Destroy(child.gameObject);
        }
        noteToUiNoteMap.Clear();
    }

    private void CreateUiNote(Note note)
    {
        if (note.StartBeat == note.EndBeat)
        {
            return;
        }

        UiNote uiNote = Instantiate(uiNotePrefab, uiNotesContainer);
        uiNote.Init(note, uiEffectsContainer);
        if (micProfile != null)
        {
            uiNote.SetColorOfMicProfile(micProfile);
        }

        Text uiNoteText = uiNote.lyricsUiText;
        string pitchName = MidiUtils.GetAbsoluteName(note.MidiNote);
        if (settings.GraphicSettings.showLyricsOnNotes && showPitchOfNotes)
        {
            uiNoteText.text = GetDisplayText(note) + " (" + pitchName + ")";
        }
        else if (settings.GraphicSettings.showLyricsOnNotes)
        {
            uiNoteText.text = GetDisplayText(note);
        }
        else if (showPitchOfNotes)
        {
            uiNoteText.text = pitchName;
        }
        else
        {
            uiNoteText.text = "";
        }

        RectTransform uiNoteRectTransform = uiNote.RectTransform;
        PositionUiNote(uiNoteRectTransform, note.MidiNote, note.StartBeat, note.EndBeat);

        noteToUiNoteMap[note] = uiNote;
    }

    public string GetDisplayText(Note note)
    {
        switch (note.Type)
        {
            case ENoteType.Freestyle:
                return $"<i><b><color=#c00000>{note.Text}</color></b></i>";
            case ENoteType.Golden:
                return $"<b>{note.Text}</b>";
            case ENoteType.Rap:
            case ENoteType.RapGolden:
                return $"<i><b><color=#ffa500ff>{note.Text}</color></b></i>";
            default:
                return note.Text;
        }
    }

    private void RemoveUiRecordedNotes()
    {
        foreach (Transform child in uiRecordedNotesContainer.transform)
        {
            Destroy(child.gameObject);
        }
        recordedNoteToUiRecordedNotesMap.Clear();
    }

    private void CreateUiRecordedNote(RecordedNote recordedNote, bool useRoundedMidiNote)
    {
        if (recordedNote.StartBeat == recordedNote.EndBeat)
        {
            return;
        }

        int midiNote = (useRoundedMidiNote) ? recordedNote.RoundedMidiNote : recordedNote.RecordedMidiNote;

        UiRecordedNote uiNote = Instantiate(uiRecordedNotePrefab, uiRecordedNotesContainer);
        uiNote.MidiNote = midiNote;
        if (micProfile != null)
        {
            uiNote.SetColorOfMicProfile(micProfile);
        }

        Text uiNoteText = uiNote.lyricsUiText;
        if (showPitchOfNotes)
        {
            string pitchName = MidiUtils.GetAbsoluteName(midiNote);
            uiNoteText.text = " (" + pitchName + ")";
        }
        else
        {
            uiNoteText.text = "";
        }

        RectTransform uiNoteRectTransform = uiNote.RectTransform;
        PositionUiNote(uiNoteRectTransform, midiNote, recordedNote.StartBeat, recordedNote.EndBeat);

        recordedNoteToUiRecordedNotesMap.AddInsideList(recordedNote, uiNote);
    }

    private void PositionUiNote(RectTransform uiNote, int midiNote, double noteStartBeat, double noteEndBeat)
    {
        // Calculate offset, such that the average note will be on the middle line
        // (thus, middle line has offset of zero).
        int offset = (noteLineCount / 2) - (avgMidiNote % noteLineCount);
        int noteLine = (offset + midiNote) % noteLineCount;

        int sentenceStartBeat = displayedSentence.MinBeat;
        int sentenceEndBeat = displayedSentence.MaxBeat;
        int beatsInSentence = sentenceEndBeat - sentenceStartBeat;

        float anchorY = (float)noteLine / noteLineCount;
        float anchorXStart = (float)(noteStartBeat - sentenceStartBeat) / beatsInSentence;
        float anchorXEnd = (float)(noteEndBeat - sentenceStartBeat) / beatsInSentence;

        uiNote.anchorMin = new Vector2(anchorXStart, anchorY);
        uiNote.anchorMax = new Vector2(anchorXEnd, anchorY);
        uiNote.MoveCornersToAnchors_Width();
        uiNote.MoveCornersToAnchors_CenterPosition();
    }

    public void CreatePerfectSentenceEffect()
    {
        for (int i = 0; i < 50; i++)
        {
            CreatePerfectSentenceStar();
        }
    }

    private void CreatePerfectSentenceStar()
    {
        StarParticle star = Instantiate(perfectSentenceStarPrefab, uiEffectsContainer);
        RectTransform starRectTransform = star.GetComponent<RectTransform>();
        float anchorX = UnityEngine.Random.Range(0f, 1f);
        float anchorY = UnityEngine.Random.Range(0f, 1f);
        starRectTransform.anchorMin = new Vector2(anchorX, anchorY);
        starRectTransform.anchorMax = new Vector2(anchorX, anchorY);
        starRectTransform.anchoredPosition = Vector2.zero;

        star.RectTransform.localScale = Vector3.one * UnityEngine.Random.Range(0.2f, 0.6f);
        LeanTween.scale(star.RectTransform, Vector3.zero, 1f)
            .setOnComplete(() => Destroy(star.gameObject));
    }
}
