using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ScrollingNoteStreamDisplayer : AbstractSingSceneNoteDisplayer
{
    private const float PitchIndicatorXPercent = 0.2f;
    private const float DisplayedNoteDurationInSeconds = 5;
    private const float DisplayedNoteDurationInMillis = DisplayedNoteDurationInSeconds * 1000;

    private const double ResetPitchDistanceThresholdInMillis = 1200;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private Voice voice;

    private List<Note> upcomingNotes = new();
    private List<Sentence> upcomingSentences = new();

    private int delayInMillis;
    private int displayedBeats;

    private int frameCount;

    private readonly Dictionary<Note, Label> noteToLyricsContainerLabel = new();

    private readonly Dictionary<Sentence, VisualElement> sentenceToSeparator = new();

    private readonly Dictionary<Note, int> noteToPrecalculatedNoteRowUnwrapped = new();

    private int DefaultNoteRow => noteRowCount / 2;

    private readonly Dictionary<BeatRange, Note> beatRangeToNoteOrPrevious = new();
    private readonly Dictionary<int, Note> beatToNote = new();

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();

        if (micProfile != null)
        {
            delayInMillis = micProfile.IsInputFromConnectedClient
                ? micProfile.DelayInMillis + settings.CompanionClientMessageBufferTimeInMillis
                : micProfile.DelayInMillis;
            effectsContainer.Add(CreateRecordingPositionIndicator());
        }

        upcomingNotes = voice.Sentences
            .SelectMany(sentence => sentence.Notes)
            .Where(note => medleyControl.IsNoteInMedleyRange(note))
            .ToList();
        upcomingNotes.Sort(Note.comparerByStartBeat);
        upcomingSentences = voice.Sentences.ToList();

        avgMidiNote = CalculateAvgMidiNote(voice.Sentences.SelectMany(sentence => sentence.Notes).ToList());
        maxNoteRowMidiNote = avgMidiNote + (noteRowCount / 2);
        minNoteRowMidiNote = avgMidiNote - (noteRowCount / 2);

        displayedBeats = (int)Math.Ceiling(SongMetaBpmUtils.BeatsPerSecond(songMeta) * DisplayedNoteDurationInSeconds);
    }

    public override void SetLineCount(int theLineCount)
    {
        base.SetLineCount(theLineCount);


        PrecalculateNoteRowUnwrapped();
        PrecalculateBeatRangeToNote();
    }

    private void PrecalculateBeatRangeToNote()
    {
        // Beat range of current note is from start of current note (inclusive) to start of following note (exclusive).
        Note previousNote = null;
        foreach (Note currentNote in upcomingNotes)
        {
            if (currentNote != null
                && previousNote != null)
            {
                beatRangeToNoteOrPrevious.Add(new BeatRange(previousNote.StartBeat, currentNote.StartBeat), previousNote);
            }
            previousNote = currentNote;
        }

        if (!upcomingNotes.IsNullOrEmpty())
        {
            Note finalNote = upcomingNotes.LastOrDefault();
            beatRangeToNoteOrPrevious.Add(new BeatRange(finalNote.StartBeat, finalNote.EndBeat), finalNote);
        }
    }

    private void PrecalculateNoteRowUnwrapped()
    {
        if (settings.NoteDisplayLineCount <= 0)
        {
            // "All" lines should be displayed. Thus, there are enough lines to map a MidiNote to a NoteRow.
            PrecalculateNoteRowUnwrappedByAbsoluteMidiNotePositions();
        }
        else
        {
            PrecalculateNoteRowUnwrappedByRelativeMidiNoteDistance();
        }
    }

    private void PrecalculateNoteRowUnwrappedByRelativeMidiNoteDistance()
    {
        Note previousNote = null;
        List<Note> currentBatch = new();
        foreach (Note currentNote in upcomingNotes)
        {
            if (SongMetaUtils.TryGetDistanceInMillis(songMeta, currentNote, previousNote, out double distanceInMillis)
                && distanceInMillis > ResetPitchDistanceThresholdInMillis
                && currentNote.StartBeat >= previousNote.EndBeat)
            {
                // Start a new batch of notes
                // Debug.Log($"start new batch of notes: {currentNote.Text}@{currentNote.StartBeat}");
                ShiftPrecalculatedNoteRowUnwrappedToMinimizeWrapping(currentBatch);
                currentBatch.Clear();
                previousNote = null;
            }

            currentBatch.Add(currentNote);
            int noteRow = PrecalculateNoteRowUnwrapped(upcomingNotes, currentNote, previousNote);
            noteToPrecalculatedNoteRowUnwrapped[currentNote] = noteRow;
            previousNote = currentNote;
        }
    }

    private void PrecalculateNoteRowUnwrappedByAbsoluteMidiNotePositions()
    {
        int minMidiNote = SongMetaUtils.GetMaxMidiNote(upcomingNotes);
        foreach (Note currentNote in upcomingNotes)
        {
            noteToPrecalculatedNoteRowUnwrapped[currentNote] = currentNote.MidiNote - minMidiNote;
        }
        ShiftPrecalculatedNoteRowUnwrappedToMinimizeWrapping(upcomingNotes);
    }

    private void ShiftPrecalculatedNoteRowUnwrappedToMinimizeWrapping(List<Note> notesInBatch)
    {
        if (notesInBatch.IsNullOrEmpty()
            || noteToPrecalculatedNoteRowUnwrapped.IsNullOrEmpty())
        {
            return;
        }

        // Calculate overshoot / undershoot
        HashSet<int> noteRowsInBatch = notesInBatch
            .Select(note =>
            {
                if (noteToPrecalculatedNoteRowUnwrapped.TryGetValue(note, out int noteRow))
                {
                    return noteRow;
                }
                return DefaultNoteRow;
            })
            .ToHashSet();
        int maxNoteRow = noteRowsInBatch.Max();
        int minNoteRow = noteRowsInBatch.Min();

        int topNoteRow = (noteRowCount - 1);
        int bottomNoteRow = 0;

        int topNoteRowSpace = topNoteRow - maxNoteRow;
        int bottomNoteRowSpace = minNoteRow - bottomNoteRow;

        int topNoteRowOvershoot = topNoteRowSpace < 0
            ? topNoteRowSpace
            : 0;
        bool isTopOvershoot = topNoteRowOvershoot < 0;

        int bottomNoteRowOvershoot = bottomNoteRowSpace < 0
            ? minNoteRow
            : 0;
        bool isBottomOvershoot = bottomNoteRowOvershoot < 0;

        if (isTopOvershoot
            && isBottomOvershoot)
        {
            // Cannot be shifted to remove wrapping
            // Debug.Log($"Cannot remove wrapping because top and bottom overshoot: {SongMetaUtils.GetLyrics(notesInBatch)} (min note row: {minNoteRow}, max note row: {maxNoteRow})");
            return;
        }

        int noteRowShift;
        if (!isTopOvershoot
            && !isBottomOvershoot)
        {
            // Shift to center, i.e. try to make top and bottom space equal
            noteRowShift = (topNoteRowSpace - bottomNoteRowSpace) / 2;
            // if (noteRowShift != 0)
            // {
            //     Debug.Log($"Shifted to center notes: {SongMetaUtils.GetLyrics(notesInBatch)} (old min note row: {minNoteRow}, old max note row: {maxNoteRow}, shift: {noteRowShift}, noteRowCount: {noteRowCount})");
            // }
        }
        else if (isTopOvershoot)
        {
            noteRowShift = topNoteRowOvershoot;
            int minNoteRowShift = -bottomNoteRowSpace;
            if (noteRowShift < minNoteRowShift)
            {
                // Debug.Log($"Cannot remove wrapping completely: {SongMetaUtils.GetLyrics(notesInBatch)}");
                noteRowShift = minNoteRowShift;
            }
            // Debug.Log($"Shifted downwards to reduce wrapping at top: {SongMetaUtils.GetLyrics(notesInBatch)} (old min note row: {minNoteRow}, old max note row: {maxNoteRow}, shift: {noteRowShift}, noteRowCount: {noteRowCount})");
        }
        else
        {
            noteRowShift = -bottomNoteRowOvershoot;
            int maxNoteRowShift = topNoteRowSpace;
            if (noteRowShift > maxNoteRowShift)
            {
                // Debug.Log($"Cannot remove wrapping completely: {SongMetaUtils.GetLyrics(notesInBatch)}");
                noteRowShift = maxNoteRowShift;
            }
            // Debug.Log($"Shifted upwards to reduce wrapping at bottom: {SongMetaUtils.GetLyrics(notesInBatch)} (old min note row: {minNoteRow}, old max note row: {maxNoteRow}, shift: {noteRowShift}, noteRowCount: {noteRowCount})");
        }

        ShiftPrecalculatedNoteRowUnwrapped(notesInBatch, noteRowShift);
    }

    private void ShiftPrecalculatedNoteRowUnwrapped(List<Note> notesInBatch, int noteRowShift)
    {
        if (noteRowShift == 0
            || notesInBatch.IsNullOrEmpty())
        {
            return;
        }

        foreach (Note note in notesInBatch)
        {
            if (noteToPrecalculatedNoteRowUnwrapped.TryGetValue(note, out int unshiftedNoteRow))
            {
                int shiftedNoteRow = unshiftedNoteRow + noteRowShift;
                noteToPrecalculatedNoteRowUnwrapped[note] = shiftedNoteRow;
            }
        }
    }

    protected override int CalculateNoteRow(int midiNote, int beat)
    {
        Note note = GetNoteOrPreviousAtBeat(beat);
        if (note == null
            || noteToPrecalculatedNoteRowUnwrapped.IsNullOrEmpty())
        {
            // Debug.Log($"Fallback to default note row (beat: {beat}, note: {note})");
            return DefaultNoteRow;
        }

        if (noteToPrecalculatedNoteRowUnwrapped.TryGetValue(note, out int noteRow))
        {
            int targetMidiNote = note.MidiNote;
            if (midiNote == targetMidiNote)
            {
                return noteRow;
            }

            float relativePitchDistance = MidiUtils.GetRelativePitchDistance(targetMidiNote, midiNote);
            if (relativePitchDistance <= 0.5f)
            {
                return noteRow;
            }

            int noteRowOffsetDirection = -NumberUtils.ShortestCircleDirection(
                midiNote,
                targetMidiNote,
                MidiUtils.NoteCountInAnOctave);

            int noteRowOffset = (int)Math.Min(relativePitchDistance, 2);

            int offsetNoteRow = noteRow + (noteRowOffset * noteRowOffsetDirection);
            return offsetNoteRow;
        }

        return DefaultNoteRow;
    }

    private Note GetNoteOrPreviousAtBeat(int beat)
    {
        if (beatToNote.TryGetValue(beat, out Note cachedNote))
        {
            return cachedNote;
        }

        // TODO: binary search for better performance.
        BeatRange beatRange = beatRangeToNoteOrPrevious.Keys.FirstOrDefault(beatRange => beatRange.StartBeatInclusive <= beat && beat < beatRange.EndBeatExclusive);
        if (beatRange.StartBeatInclusive <= 0
            && beatRange.EndBeatExclusive <= 0)
        {
            return null;
        }

        if (beatRangeToNoteOrPrevious.TryGetValue(beatRange, out Note note))
        {
            beatToNote[beat] = note;
            return note;
        }

        return null;
    }

    private int PrecalculateNoteRowUnwrapped(List<Note> notes, Note currentNote, Note previousNote)
    {
        if (currentNote == null
            || previousNote == null)
        {
            // Start with the center row
            // Debug.Log($"start at noteRow: {startNoteRow}: {currentNote.Text}@{currentNote.StartBeat}");
            return DefaultNoteRow;
        }

        int midiNoteDifference = currentNote.MidiNote - previousNote.MidiNote;
        int midiNoteDistance = Math.Abs(midiNoteDifference);
        if (noteToPrecalculatedNoteRowUnwrapped.TryGetValue(previousNote, out int previousNoteRow))
        {
            int noteRowCountStep = GetNoteRowCountStep(midiNoteDistance);
            if (midiNoteDifference > 0)
            {
                int resultNoteRow = previousNoteRow + noteRowCountStep;
                // Debug.Log($"midiNoteDifference {previousNote.Text}@{previousNote.StartBeat} -> {currentNote.Text}@{currentNote.StartBeat} = {midiNoteDifference}, row-step: {noteRowCountStep}, old-res: {previousNoteRow}, res: {resultNoteRow}");
                return resultNoteRow;
            }
            else if (midiNoteDifference < 0)
            {
                int resultNoteRow = previousNoteRow - noteRowCountStep;
                // Debug.Log($"midiNoteDifference {previousNote.Text}@{previousNote.StartBeat} -> {currentNote.Text}@{currentNote.StartBeat} = {midiNoteDifference}, row-step: {noteRowCountStep}, old-res: {previousNoteRow}, res: {resultNoteRow}");
                return resultNoteRow;
            }
            else
            {
                return previousNoteRow;
            }
        }

        // Fallback to the center row
        // Debug.Log($"fallback to center row: {currentNote.Text}@{currentNote.StartBeat}");
        return DefaultNoteRow;
    }

    private int GetNoteRowCountStep(int midiNoteDistance)
    {
        if (midiNoteDistance <= 0)
        {
            return 0;
        }
        else if (midiNoteDistance <= 1)
        {
            return 1;
        }
        else if (midiNoteDistance <= 3)
        {
            return 2;
        }
        else if (midiNoteDistance <= 5)
        {
            return 3;
        }
        else
        {
            return 4;
        }
    }

    public override void Update()
    {
        base.Update();
        RemoveNotesOutsideOfDisplayArea();
        CreateNotesInDisplayArea();

        sentenceToSeparator.ForEach(entry =>
        {
            Sentence sentence = entry.Key;
            VisualElement separator = entry.Value;
            UpdateSeparatorPosition(separator, sentence);
        });
    }

    protected override void UpdateTargetNoteControl(TargetNoteControl targetNoteControl, int indexInList)
    {
        UpdateNotePosition(targetNoteControl.VisualElement, targetNoteControl.Note.MidiNote, targetNoteControl.Note.StartBeat, targetNoteControl.Note.EndBeat);
        UpdateTargetNoteLabelWith(targetNoteControl, indexInList);
    }

    protected override void UpdateRecordedNoteControl(RecordedNoteControl recordedNoteControl)
    {
        base.UpdateRecordedNoteControl(recordedNoteControl);

        if (recordedNoteControl.EndBeat >= recordedNoteControl.TargetEndBeat)
        {
            // The other case (EndBeat < TargetEndBeat) is handled in the base method.
            UpdateNotePosition(recordedNoteControl.VisualElement, recordedNoteControl.MidiNote, recordedNoteControl.StartBeat, recordedNoteControl.EndBeat);
        }
    }

    public override float GetXInPercent(double positionInMillis)
    {
        // The VerticalPitchIndicator's position is the position in the song (where players should be singing now).
        double offsetInMillis = positionInMillis - songAudioPlayer.PositionInMillis;
        float offsetInPercent = (float)(offsetInMillis / DisplayedNoteDurationInMillis);
        return PitchIndicatorXPercent + offsetInPercent;
    }

    protected override bool TryGetNotePositionInPercent(VisualElement visualElement, int midiNote, double noteStartBeat, double noteEndBeat, out Rect result)
    {
        // The VerticalPitchIndicator's position is the position in the song (where players should be singing now).
        double millisInSong = songAudioPlayer.PositionInMillis;

        // Alternative: The VerticalPitchIndicator's position is the position where recording happens.
        // double millisInSong = songAudioPlayer.PositionInMillis - delayInMillis;
        double currentBeatConsideringMicDelay = SongMetaBpmUtils.MillisToBeats(songMeta, millisInSong);

        Vector2 yStartEndPercent = GetYStartAndEndInPercentForMidiNote(midiNote, (int)noteStartBeat);
        float yStartPercent = yStartEndPercent.x;
        float yEndPercent = yStartEndPercent.y;
        float xStartPercent = (float)((noteStartBeat - currentBeatConsideringMicDelay) / displayedBeats) + PitchIndicatorXPercent;
        float xEndPercent = (float)((noteEndBeat - currentBeatConsideringMicDelay) / displayedBeats) + PitchIndicatorXPercent;

        yStartPercent *= 100;
        yEndPercent *= 100;
        xStartPercent *= 100;
        xEndPercent *= 100;
        result = new Rect(xStartPercent, yStartPercent, xEndPercent - xStartPercent, yEndPercent - yStartPercent);
        return true;
    }

    protected override TargetNoteControl CreateTargetNoteControl(Note note)
    {
        TargetNoteControl targetNoteControl = base.CreateTargetNoteControl(note);
        if (targetNoteControl == null)
        {
            return null;
        }

        return targetNoteControl;
    }

    private void CreateNotesInDisplayArea()
    {
        // Create UiNotes to fill the display area
        int displayAreaMinBeat = CalculateDisplayAreaMinBeat();
        int displayAreaMaxBeat = CalculateDisplayAreaMaxBeat();

        List<Note> newNotes = new();
        foreach (Note note in upcomingNotes)
        {
            if (displayAreaMinBeat <= note.StartBeat && note.StartBeat <= displayAreaMaxBeat)
            {
                newNotes.Add(note);
            }
            else if (note.StartBeat > displayAreaMaxBeat)
            {
                // The upcoming notes are sorted. Thus, all following notes will not be inside the drawingArea as well.
                break;
            }
        }

        // Create UiNotes
        foreach (Note note in newNotes)
        {
            // The note is not upcoming anymore
            upcomingNotes.Remove(note);
            CreateTargetNoteControl(note);
        }

        // Create sentence separators
        List<Sentence> newSentences = new();
        foreach (Sentence sentence in upcomingSentences)
        {
            if (sentenceToSeparator.ContainsKey(sentence))
            {
                continue;
            }

            if (displayAreaMinBeat <= sentence.MinBeat && sentence.ExtendedMaxBeat <= displayAreaMaxBeat
                && medleyControl.IsSentenceInMedleyRange(sentence))
            {
                VisualElement separator = CreateSentenceSeparator(sentence);
                UpdateSeparatorPosition(separator, sentence);
                targetNoteEntryContainer.Add(separator);
                newSentences.Add(sentence);
            }
            else if (sentence.ExtendedMaxBeat > displayAreaMaxBeat)
            {
                // The upcoming sentence are sorted. Thus, all following sentence will not be inside the drawingArea as well.
                break;
            }
        }
        newSentences.ForEach(sentence => upcomingSentences.Remove(sentence));
    }

    private void UpdateSeparatorPosition(VisualElement separator, Sentence sentence)
    {
        UpdateNotePosition(separator, 0, sentence.LinebreakBeat, sentence.LinebreakBeat);
        float marginTopBottomInPercent = 5;
        separator.style.top = new StyleLength(new Length(marginTopBottomInPercent, LengthUnit.Percent));
        separator.style.width = 1;
        separator.style.height = new StyleLength(new Length(90 - marginTopBottomInPercent * 2, LengthUnit.Percent));
    }

    private VisualElement CreateSentenceSeparator(Sentence sentence)
    {
        VisualElement separator = new();
        separator.AddToClassList("scrollingNoteStreamSentenceSeparator");
        sentenceToSeparator[sentence] = separator;
        return separator;
    }

    private void RemoveNotesOutsideOfDisplayArea()
    {
        int displayAreaMinBeat = CalculateDisplayAreaMinBeat();
        foreach (TargetNoteControl targetNoteControl in noteToTargetNoteControl.Values.ToList())
        {
            if (targetNoteControl.Note.EndBeat < displayAreaMinBeat)
            {
                RemoveTargetNote(targetNoteControl);
            }
        }

        List<RecordedNoteControl> recordedNoteControls =
            recordedNoteToRecordedNoteControlsMap
                .SelectMany(entry => entry.Value)
                .ToList();
        foreach (RecordedNoteControl recordedNoteControl in recordedNoteControls)
        {
            if (recordedNoteControl.EndBeat < displayAreaMinBeat)
            {
                RemoveRecordedNote(recordedNoteControl);
            }
        }

        List<Sentence> sentencesToBeRemoved = new();
        sentenceToSeparator.Keys.ForEach(sentence =>
        {
            if (sentence.ExtendedMaxBeat < displayAreaMinBeat)
            {
                sentencesToBeRemoved.Add(sentence);
            }
        });
        sentencesToBeRemoved.ForEach(sentence => RemoveSentenceSeparator(sentence));
    }

    private void RemoveSentenceSeparator(Sentence sentence)
    {
        if (!sentenceToSeparator.TryGetValue(sentence, out VisualElement separator))
        {
            return;
        }
        sentenceToSeparator.Remove(sentence);
        separator.RemoveFromHierarchy();
    }

    private static int CalculateAvgMidiNote(IReadOnlyCollection<Note> notes)
    {
        return notes.Count > 0
            ? (int)notes.Select(it => it.MidiNote).Average()
            : 0;
    }

    private int CalculateDisplayAreaMinBeat()
    {
        // This is an over-approximation of the visible displayArea
        return (int)songAudioPlayer.GetCurrentBeat(false) - displayedBeats / 2;
    }

    private int CalculateDisplayAreaMaxBeat()
    {
        // This is an over-approximation of the visible displayArea
        return (int)songAudioPlayer.GetCurrentBeat(false) + displayedBeats;
    }

    private VisualElement CreateRecordingPositionIndicator()
    {
        VisualElement recordingPositionIndicator = new();
        recordingPositionIndicator.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        recordingPositionIndicator.style.position = new StyleEnum<Position>(Position.Absolute);
        recordingPositionIndicator.style.width = new StyleLength(new Length(1, LengthUnit.Pixel));
        recordingPositionIndicator.style.height = new StyleLength(new Length(100, LengthUnit.Percent));
        recordingPositionIndicator.style.left = new StyleLength(new Length(PitchIndicatorXPercent * 100, LengthUnit.Percent));;
        return recordingPositionIndicator;
    }

    protected override void RemoveTargetNote(TargetNoteControl targetNoteControl)
    {
        base.RemoveTargetNote(targetNoteControl);
        if (noteToLyricsContainerLabel.TryGetValue(targetNoteControl.Note, out Label label))
        {
            label.RemoveFromHierarchy();
            noteToLyricsContainerLabel.Remove(targetNoteControl.Note);
        }
    }

    private struct BeatRange
    {
        public int StartBeatInclusive { get; private set; }
        public int EndBeatExclusive { get; private set; }

        public BeatRange(int startBeatInclusive, int endBeatExclusive)
        {
            this.StartBeatInclusive = startBeatInclusive;
            this.EndBeatExclusive = endBeatExclusive;
        }
    }
}
