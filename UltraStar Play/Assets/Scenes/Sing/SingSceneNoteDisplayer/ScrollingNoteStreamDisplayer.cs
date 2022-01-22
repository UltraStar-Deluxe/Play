using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using UnityEngine.UIElements;
using UnityEngine.VFX;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class ScrollingNoteStreamDisplayer : AbstractSingSceneNoteDisplayer
{
    // [InjectedInInspector]
    // public RectTransform lyricsBar;
    //
    // public float pitchIndicatorAnchorX = 0.15f;
    // public float displayedNoteDurationInSeconds = 5;
    //
    // [Inject]
    // private SongAudioPlayer songAudioPlayer;
    //
    // [Inject]
    // private Voice voice;
    //
    // private List<Note> upcomingNotes = new List<Note>();
    //
    // private int micDelayInMillis;
    // private int displayedBeats;
    //
    // private int frameCount;
    //
    // private void Update()
    // {
    //     // For some reason, Unity seems to need some frames to finish the calculation of the lyricsBar position.
    //     // In the first frame, the lyrics positions are wrong. Thus, as a workaround, delay the Update code by one frame.
    //     if (frameCount > 2)
    //     {
    //         RemoveNotesOutsideOfDisplayArea();
    //         CreateNotesInDisplayArea();
    //
    //         UpdateUiNotePositions();
    //     }
    //     else
    //     {
    //         frameCount++;
    //     }
    // }
    //
    // override public void Init(int lineCount)
    // {
    //     if (micProfile != null)
    //     {
    //         micDelayInMillis = micProfile.DelayInMillis;
    //     }
    //     base.Init(lineCount);
    //
    //     upcomingNotes = voice.Sentences
    //         .SelectMany(sentence => sentence.Notes)
    //         .ToList();
    //     upcomingNotes.Sort(Note.comparerByStartBeat);
    //
    //     avgMidiNote = CalculateAvgMidiNote(voice.Sentences.SelectMany(sentence => sentence.Notes).ToList());
    //     maxNoteRowMidiNote = avgMidiNote + (noteRowCount / 2);
    //     minNoteRowMidiNote = avgMidiNote - (noteRowCount / 2);
    //
    //     displayedBeats = (int)Math.Ceiling(BpmUtils.GetBeatsPerSecond(songMeta) * displayedNoteDurationInSeconds);
    // }
    //
    // protected override void UpdateNotePosition(VisualElement visualElement, int midiNote, double noteStartBeat, double noteEndBeat)
    // {
    //     // The VerticalPitchIndicator's position is the position where recording happens.
    //     // Thus, a note with startBeat == (currentBeat + micDelayInBeats) will have its left side drawn where the VerticalPitchIndicator is.
    //     double millisInSong = songAudioPlayer.PositionInSongInMillis - micDelayInMillis;
    //     double currentBeatConsideringMicDelay = BpmUtils.MillisecondInSongToBeat(songMeta, millisInSong);
    //
    //     Vector2 yStartEndPercent = GetYStartAndEndInPercentForMidiNote(midiNote);
    //     float yStartPercent = yStartEndPercent.x;
    //     float yEndPercent = yStartEndPercent.y;
    //     float xStartPercent = (float)((noteStartBeat - currentBeatConsideringMicDelay) / displayedBeats) + pitchIndicatorAnchorX;
    //     float xEndPercent = (float)((noteEndBeat - currentBeatConsideringMicDelay) / displayedBeats) + pitchIndicatorAnchorX;
    //
    //     visualElement.style.left = new StyleLength(new Length(xStartPercent, LengthUnit.Percent));
    //     visualElement.style.width = new StyleLength(new Length(xEndPercent - xStartPercent, LengthUnit.Percent));
    //     visualElement.style.top = new StyleLength(new Length(yStartPercent, LengthUnit.Percent));
    //     visualElement.style.height = new StyleLength(new Length(yEndPercent - yStartPercent, LengthUnit.Percent));
    // }
    //
    // override protected TargetNoteControl CreateUiNote(Note note)
    // {
    //     TargetNoteControl targetNoteControl = base.CreateUiNote(note);
    //     if (targetNoteControl != null)
    //     {
    //         // Freestyle notes are not drawn
    //         if (targetNoteControl.Note.IsFreestyle)
    //         {
    //             targetNoteControl.image.enabled = false;
    //         }
    //
    //         PositionUiNoteLyrics(targetNoteControl);
    //     }
    //     return targetNoteControl;
    // }
    //
    // private void PositionUiNoteLyrics(TargetNoteControl targetNoteControl)
    // {
    //     // Position lyrics. Width until next note, vertically centered on lyricsBar.
    //     targetNoteControl.label.enabled = true;
    //     targetNoteControl.label.color = Color.white;
    //     targetNoteControl.label.alignment = TextAnchor.MiddleLeft;
    //
    //     RectTransform lyricsRectTransform = targetNoteControl.lyricsUiTextRectTransform;
    //     lyricsRectTransform.SetParent(targetNoteControl.transform.parent, true);
    //     PositionUiNote(lyricsRectTransform, 60, targetNoteControl.Note.StartBeat, GetNoteStartBeatOfFollowingNote(targetNoteControl.Note));
    //     lyricsRectTransform.SetParent(lyricsBar, true);
    //     lyricsRectTransform.anchorMin = new Vector2(lyricsRectTransform.anchorMin.x, 0);
    //     lyricsRectTransform.anchorMax = new Vector2(lyricsRectTransform.anchorMax.x, 1);
    //     lyricsRectTransform.sizeDelta = new Vector2(lyricsRectTransform.sizeDelta.x, 0);
    //     lyricsRectTransform.localPosition = new Vector2(lyricsRectTransform.localPosition.x, 0);
    //     targetNoteControl.label.transform.SetParent(lyricsBar, true);
    // }
    //
    // private double GetNoteStartBeatOfFollowingNote(Note note)
    // {
    //     Sentence sentence = note.Sentence;
    //     if (sentence == null)
    //     {
    //         return note.EndBeat;
    //     }
    //
    //     Note followingNote = sentence.Notes
    //         .Where(otherNote => otherNote.StartBeat >= note.EndBeat)
    //         .OrderBy(otherNote => otherNote.StartBeat)
    //         .FirstOrDefault();
    //     if (followingNote != null)
    //     {
    //         if (note.EndBeat == followingNote.StartBeat)
    //         {
    //             return note.EndBeat;
    //         }
    //         else
    //         {
    //             // Add a little bit spacing
    //             return followingNote.StartBeat - 1;
    //         }
    //     }
    //     else
    //     {
    //         return sentence.ExtendedMaxBeat;
    //     }
    // }
    //
    // private void UpdateUiNotePositions()
    // {
    //     foreach (TargetNoteControl uiNote in noteToTargetNoteControl.Values)
    //     {
    //         Vector3 lastPosition = uiNote.RectTransform.position;
    //         PositionUiNote(uiNote.RectTransform, uiNote.Note.MidiNote, uiNote.Note.StartBeat, uiNote.Note.EndBeat);
    //         Vector3 positionDelta = uiNote.RectTransform.position - lastPosition;
    //         uiNote.lyricsUiTextRectTransform.Translate(positionDelta);
    //     }
    //
    //     foreach (RecordedNoteControl uiRecordedNote in RecordedNoteControls)
    //     {
    //         // Draw the UiRecordedNotes smoothly from their StartBeat to TargetEndBeat
    //         if (uiRecordedNote.EndBeat < uiRecordedNote.TargetEndBeat)
    //         {
    //             UpdateUiRecordedNoteEndBeat(uiRecordedNote);
    //         }
    //
    //         PositionUiNote(uiRecordedNote.RectTransform, uiRecordedNote.MidiNote, uiRecordedNote.StartBeat, uiRecordedNote.EndBeat);
    //     }
    // }
    //
    // private void CreateNotesInDisplayArea()
    // {
    //     // Create UiNotes to fill the display area
    //     int displayAreaMinBeat = CalculateDisplayAreaMinBeat();
    //     int displayAreaMaxBeat = CalculateDisplayAreaMaxBeat();
    //
    //     List<Note> newNotes = new List<Note>();
    //     foreach (Note note in upcomingNotes)
    //     {
    //         if (displayAreaMinBeat <= note.StartBeat && note.StartBeat <= displayAreaMaxBeat)
    //         {
    //             newNotes.Add(note);
    //         }
    //         else if (note.StartBeat > displayAreaMaxBeat)
    //         {
    //             // The upcoming notes are sorted. Thus, all following notes will not be inside the drawingArea as well.
    //             break;
    //         }
    //     }
    //
    //     // Create UiNotes
    //     foreach (Note note in newNotes)
    //     {
    //         // The note is not upcoming anymore
    //         upcomingNotes.Remove(note);
    //         CreateUiNote(note);
    //     }
    // }
    //
    // private void RemoveNotesOutsideOfDisplayArea()
    // {
    //     int displayAreaMinBeat = CalculateDisplayAreaMinBeat();
    //     foreach (TargetNoteControl uiNote in noteToTargetNoteControl.Values.ToList())
    //     {
    //         if (uiNote.Note.EndBeat < displayAreaMinBeat)
    //         {
    //             RemoveTargetNote(uiNote);
    //         }
    //     }
    //     foreach (RecordedNoteControl uiRecordedNote in RecordedNoteControls.ToList())
    //     {
    //         if (uiRecordedNote.EndBeat < displayAreaMinBeat)
    //         {
    //             RemoveRecordedNote(uiRecordedNote);
    //         }
    //     }
    // }
    //
    // private int CalculateAvgMidiNote(IReadOnlyCollection<Note> notes)
    // {
    //     return notes.Count > 0
    //         ? (int)notes.Select(it => it.MidiNote).Average()
    //         : 0;
    // }
    //
    // private int CalculateDisplayAreaMinBeat()
    // {
    //     // This is an over-approximation of the visible displayArea
    //     return (int)songAudioPlayer.CurrentBeat - displayedBeats / 2;
    // }
    //
    // private int CalculateDisplayAreaMaxBeat()
    // {
    //     // This is an over-approximation of the visible displayArea
    //     return (int)songAudioPlayer.CurrentBeat + displayedBeats;
    // }

    protected override void UpdateNotePosition(VisualElement visualElement, int midiNote, double noteStartBeat, double noteEndBeat)
    {

    }
}
