using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UniRx;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ScrollingNoteStreamDisplayer : AbstractSingSceneNoteDisplayer
{
    private const float PitchIndicatorXPercent = 0.15f;
    private const float DisplayedNoteDurationInSeconds = 5;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private Voice voice;

    [Inject(UxmlName = R.UxmlNames.lyricsContainer)]
    private VisualElement lyricsContainer;

    private List<Note> upcomingNotes = new List<Note>();
    private List<Sentence> upcomingSentences = new List<Sentence>();

    private int micDelayInMillis;
    private int displayedBeats;

    private int frameCount;

    private readonly Dictionary<Note, Label> noteToLyricsContainerLabel = new Dictionary<Note, Label>();

    private readonly Dictionary<Sentence, VisualElement> sentenceToSeparator = new Dictionary<Sentence, VisualElement>();

    public override void OnInjectionFinished()
    {
        base.OnInjectionFinished();

        if (micProfile != null)
        {
            micDelayInMillis = micProfile.DelayInMillis;
            effectsContainer.Add(CreateRecordingPositionIndicator());
        }

        upcomingNotes = voice.Sentences
            .SelectMany(sentence => sentence.Notes)
            .ToList();
        upcomingNotes.Sort(Note.comparerByStartBeat);
        upcomingSentences = voice.Sentences.ToList();

        avgMidiNote = CalculateAvgMidiNote(voice.Sentences.SelectMany(sentence => sentence.Notes).ToList());
        maxNoteRowMidiNote = avgMidiNote + (noteRowCount / 2);
        minNoteRowMidiNote = avgMidiNote - (noteRowCount / 2);

        displayedBeats = (int)Math.Ceiling(BpmUtils.GetBeatsPerSecond(songMeta) * DisplayedNoteDurationInSeconds);
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

    protected override void UpdateTargetNoteControl(TargetNoteControl targetNoteControl)
    {
        base.UpdateTargetNoteControl(targetNoteControl);
        UpdateNotePosition(targetNoteControl.VisualElement, targetNoteControl.Note.MidiNote, targetNoteControl.Note.StartBeat, targetNoteControl.Note.EndBeat);

        UpdateNoteLyricsPosition(targetNoteControl);
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

    protected override void UpdateNotePosition(VisualElement visualElement, int midiNote, double noteStartBeat, double noteEndBeat)
    {
        // The VerticalPitchIndicator's position is the position where recording happens.
        // Thus, a note with startBeat == (currentBeat + micDelayInBeats) will have its left side drawn where the VerticalPitchIndicator is.
        double millisInSong = songAudioPlayer.PositionInSongInMillis - micDelayInMillis;
        double currentBeatConsideringMicDelay = BpmUtils.MillisecondInSongToBeat(songMeta, millisInSong);

        Vector2 yStartEndPercent = GetYStartAndEndInPercentForMidiNote(midiNote);
        float yStartPercent = yStartEndPercent.x;
        float yEndPercent = yStartEndPercent.y;
        float xStartPercent = (float)((noteStartBeat - currentBeatConsideringMicDelay) / displayedBeats) + PitchIndicatorXPercent;
        float xEndPercent = (float)((noteEndBeat - currentBeatConsideringMicDelay) / displayedBeats) + PitchIndicatorXPercent;

        yStartPercent *= 100;
        yEndPercent *= 100;
        xStartPercent *= 100;
        xEndPercent *= 100;

        visualElement.style.position = new StyleEnum<Position>(Position.Absolute);
        visualElement.style.left = new StyleLength(new Length(xStartPercent, LengthUnit.Percent));
        visualElement.style.width = new StyleLength(new Length(xEndPercent - xStartPercent, LengthUnit.Percent));
        visualElement.style.bottom = new StyleLength(new Length(yStartPercent, LengthUnit.Percent));
        visualElement.style.height = new StyleLength(new Length(yEndPercent - yStartPercent, LengthUnit.Percent));
    }

    protected override TargetNoteControl CreateTargetNoteControl(Note note)
    {
        TargetNoteControl targetNoteControl = base.CreateTargetNoteControl(note);
        if (targetNoteControl == null)
        {
            return null;
        }

        // Create label for dedicated lyrics bar
        Label label = new Label(targetNoteControl.Note.Text.Trim());
        targetNoteControl.Label.GetClasses().ForEach(className => label.AddToClassList(className));
        lyricsContainer.Add(label);
        noteToLyricsContainerLabel[targetNoteControl.Note] = label;

        UpdateNoteLyricsPosition(targetNoteControl);
        return targetNoteControl;
    }

    private void UpdateNoteLyricsPosition(TargetNoteControl targetNoteControl)
    {
        // Position lyrics. Width until next note, vertically centered on lyricsBar.
        if (!noteToLyricsContainerLabel.TryGetValue(targetNoteControl.Note, out Label label))
        {
            return;
        }

        UpdateNotePosition(label, 60, targetNoteControl.Note.StartBeat, GetNoteStartBeatOfFollowingNote(targetNoteControl.Note));
        label.style.position = new StyleEnum<Position>(Position.Absolute);
        label.style.top = 5;
        label.style.bottom = new StyleLength(StyleKeyword.Auto);
    }

    private static double GetNoteStartBeatOfFollowingNote(Note note)
    {
        Sentence sentence = note.Sentence;
        if (sentence == null)
        {
            return note.EndBeat;
        }

        Note followingNote = sentence.Notes
            .Where(otherNote => otherNote.StartBeat >= note.EndBeat)
            .OrderBy(otherNote => otherNote.StartBeat)
            .FirstOrDefault();
        if (followingNote != null)
        {
            if (note.EndBeat == followingNote.StartBeat)
            {
                return note.EndBeat;
            }
            else
            {
                // Add a little bit spacing
                return followingNote.StartBeat - 1;
            }
        }
        else
        {
            return sentence.ExtendedMaxBeat;
        }
    }

    private void CreateNotesInDisplayArea()
    {
        // Create UiNotes to fill the display area
        int displayAreaMinBeat = CalculateDisplayAreaMinBeat();
        int displayAreaMaxBeat = CalculateDisplayAreaMaxBeat();

        List<Note> newNotes = new List<Note>();
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
        List<Sentence> newSentences = new List<Sentence>();
        foreach (Sentence sentence in upcomingSentences)
        {
            if (sentenceToSeparator.ContainsKey(sentence))
            {
                continue;
            }

            if (displayAreaMinBeat <= sentence.MinBeat && sentence.ExtendedMaxBeat <= displayAreaMaxBeat)
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
        VisualElement separator = new VisualElement();
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

        List<Sentence> sentencesToBeRemoved = new List<Sentence>();
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
        return (int)songAudioPlayer.CurrentBeat - displayedBeats / 2;
    }

    private int CalculateDisplayAreaMaxBeat()
    {
        // This is an over-approximation of the visible displayArea
        return (int)songAudioPlayer.CurrentBeat + displayedBeats;
    }

    private VisualElement CreateRecordingPositionIndicator()
    {
        VisualElement recordingPositionIndicator = new VisualElement();
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
}
