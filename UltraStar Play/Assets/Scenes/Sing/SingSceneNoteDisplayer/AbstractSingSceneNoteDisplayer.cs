using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
using System.Xml;
using TMPro;
using UniInject.Extensions;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Random = System.Random;
using Range = UnityEngine.SocialPlatforms.Range;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class AbstractSingSceneNoteDisplayer : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(perfectEffectStarUi))]
    protected VisualTreeAsset perfectEffectStarUi;

    [Inject(Key = nameof(noteUi))]
    protected VisualTreeAsset noteUi;

    [Inject(UxmlName = R.UxmlNames.targetNoteEntryContainer)]
    protected VisualElement targetNoteEntryContainer;

    [Inject(UxmlName = R.UxmlNames.recordedNoteEntryContainer)]
    protected VisualElement recordedNoteEntryContainer;

    [Inject(UxmlName = R.UxmlNames.effectsContainer)]
    protected VisualElement effectsContainer;

    [Inject]
    protected Settings settings;

    [Inject]
    protected SongMeta songMeta;

    [Inject]
    protected PlayerNoteRecorder playerNoteRecorder;

    [Inject]
    protected SingSceneControl singSceneControl;

    [Inject]
    protected Injector injector;

    [Inject(Optional = true)]
    protected MicProfile micProfile;

    // protected LineDisplayer lineDisplayer;

    protected double beatsPerSecond;

    protected readonly Dictionary<Note, TargetNoteControl> noteToTargetNoteControl = new Dictionary<Note, TargetNoteControl>();
    protected readonly Dictionary<RecordedNote, List<RecordedNoteControl>> recordedNoteToRecordedNoteControlsMap = new Dictionary<RecordedNote, List<RecordedNoteControl>>();

    protected readonly List<StarParticleControl> starControls = new List<StarParticleControl>();

    protected int avgMidiNote;

    // The number of rows on which notes can be placed.
    protected int noteRowCount;
    protected float[] noteRowToYPercent;

    protected int maxNoteRowMidiNote;
    protected int minNoteRowMidiNote;
    protected float noteHeightPercent;

    // Only for debugging
    private bool displayRoundedAndActualRecordedNotes;
    private bool showPitchOfNotes;

    protected abstract void UpdateNotePosition(VisualElement visualElement, int midiNote, double noteStartBeat, double noteEndBeat);

    public virtual void OnInjectionFinished()
    {
        targetNoteEntryContainer.Clear();
        recordedNoteEntryContainer.Clear();
        effectsContainer.Clear();

        beatsPerSecond = BpmUtils.GetBeatsPerSecond(songMeta);
        playerNoteRecorder.RecordedNoteStartedEventStream.Subscribe(recordedNoteStartedEvent =>
        {
            DisplayRecordedNote(recordedNoteStartedEvent.RecordedNote);
        });
        playerNoteRecorder.RecordedNoteContinuedEventStream.Subscribe(recordedNoteContinuedEvent =>
        {
            DisplayRecordedNote(recordedNoteContinuedEvent.RecordedNote);
        });
    }

    public virtual void Update()
    {
        // Update notes
        noteToTargetNoteControl.Values
            .ForEach(targetNoteControl => UpdateTargetNoteControl(targetNoteControl));
        recordedNoteToRecordedNoteControlsMap.Values
            .ForEach(recordedNoteControls => recordedNoteControls
                .ForEach(recordedNoteControl => UpdateRecordedNoteControl(recordedNoteControl)));

        // Update stars
        starControls.ForEach(starControl => starControl.Update());
    }

    protected virtual void UpdateTargetNoteControl(TargetNoteControl targetNoteControl)
    {
        targetNoteControl.Update();
    }

    protected virtual void UpdateRecordedNoteControl(RecordedNoteControl recordedNoteControl)
    {
        recordedNoteControl.Update();

        // Draw the RecordedNote smoothly from their StartBeat to TargetEndBeat
        if (recordedNoteControl.EndBeat < recordedNoteControl.TargetEndBeat)
        {
            UpdateRecordedNoteControlEndBeat(recordedNoteControl);
            UpdateNotePosition(recordedNoteControl.VisualElement, recordedNoteControl.MidiNote, recordedNoteControl.StartBeat, recordedNoteControl.EndBeat);
        }
    }

    public void SetLineCount(int lineCount)
    {
        // Notes can be placed on and between the drawn lines (between causes -1).
        // The first and last line is not used (which causes -2). Thus, in total it is -3.
        noteRowCount = (lineCount * 2) - 3;
        // Check that there is at least one row for every possible note in an octave.
        if (noteRowCount < 12)
        {
            throw new UnityException(this.GetType() + " must be initialized with a row count >= 12 (one row for each note in an octave)");
        }
        noteRowToYPercent = new float[noteRowCount];

        float lineHeightPercent = 1.0f / lineCount;
        noteHeightPercent = lineHeightPercent / 2.0f;

        for (int i = 0; i < noteRowCount; i++)
        {
            int lineIndex = (int)Math.Floor(i / 2f);
            // The even noteRows are drawn between the lines. Thus they have an offset of half the line height.
            float lineOffset = ((i % 2) == 0) ? 0 : (lineHeightPercent / 2);
            noteRowToYPercent[i] = ((float)lineIndex / (float)lineCount) + lineOffset + (lineHeightPercent / 2);
        }

        // lineDisplayer.SetTargetLineCount(lineCount);
    }

    public void RemoveAllDisplayedNotes()
    {
        RemoveTargetNotes();
        RemoveRecordedNotes();
    }

    protected virtual void DisplayRecordedNote(RecordedNote recordedNote)
    {
        // Freestyle notes are not drawn
        if (recordedNote.TargetNote != null
            && recordedNote.TargetNote.IsFreestyle)
        {
            return;
        }

        // Try to update existing recorded notes.
        if (recordedNoteToRecordedNoteControlsMap.TryGetValue(recordedNote, out List<RecordedNoteControl> uiRecordedNotes))
        {
            foreach (RecordedNoteControl uiRecordedNote in uiRecordedNotes)
            {
                uiRecordedNote.TargetEndBeat = recordedNote.EndBeat;
            }
            return;
        }

        // Draw the bar for the rounded note
        // and draw the bar for the actually recorded pitch if needed.
        CreateRecordedNoteControl(recordedNote, true);
        if (displayRoundedAndActualRecordedNotes
            && (recordedNote.RecordedMidiNote != recordedNote.RoundedMidiNote))
        {
            CreateRecordedNoteControl(recordedNote, false);
        }
    }

    protected virtual TargetNoteControl CreateTargetNoteControl(Note note)
    {
        if (note.StartBeat == note.EndBeat)
        {
            return null;
        }

        VisualElement visualElement = noteUi.CloneTree().Children().First();

        Injector childInjector = UniInjectUtils.CreateInjector(injector);
        childInjector.AddBindingForInstance(childInjector);
        childInjector.AddBindingForInstance(note);
        childInjector.AddBindingForInstance(Injector.RootVisualElementInjectionKey, visualElement);

        TargetNoteControl targetNoteControl = new TargetNoteControl(effectsContainer);
        childInjector.Inject(targetNoteControl);

        Label label = targetNoteControl.Label;
        string pitchName = MidiUtils.GetAbsoluteName(note.MidiNote);
        if (settings.GraphicSettings.showLyricsOnNotes && showPitchOfNotes)
        {
            label.text = GetDisplayText(note) + " (" + pitchName + ")";
        }
        else if (settings.GraphicSettings.showLyricsOnNotes)
        {
            label.text = GetDisplayText(note);
        }
        else if (showPitchOfNotes)
        {
            label.text = pitchName;
        }
        else
        {
            label.text = "";
        }

        targetNoteEntryContainer.Add(visualElement);
        UpdateNotePosition(visualElement, note.MidiNote, note.StartBeat, note.EndBeat);

        noteToTargetNoteControl[note] = targetNoteControl;

        return targetNoteControl;
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

    protected void CreateRecordedNoteControl(RecordedNote recordedNote, bool useRoundedMidiNote)
    {
        if (recordedNote.StartBeat == recordedNote.EndBeat)
        {
            return;
        }

        // Pitch detection algorithms often have issues finding the correct octave. However, the octave is irrelevant for scores.
        // When notes are drawn far away from the target note because the pitch detection got the wrong octave then it looks wrong.
        // Thus, only the relative pitch of the roundedMidiNote is used and drawn on the octave of the target note.
        int midiNote;
        if (useRoundedMidiNote
            && recordedNote.TargetNote != null)
        {
            midiNote = MidiUtils.GetMidiNoteOnOctaveOfTargetMidiNote(recordedNote.RoundedMidiNote, recordedNote.TargetNote.MidiNote);
        }
        else
        {
            midiNote = recordedNote.RecordedMidiNote;
        }

        VisualElement visualElement = noteUi.CloneTree().Children().First();

        Injector childInjector = UniInjectUtils.CreateInjector(injector);
        childInjector.AddBindingForInstance(childInjector);
        childInjector.AddBindingForInstance(recordedNote);
        childInjector.AddBindingForInstance(Injector.RootVisualElementInjectionKey, visualElement);

        RecordedNoteControl noteControl = new RecordedNoteControl();
        childInjector.Inject(noteControl);

        noteControl.StartBeat = recordedNote.StartBeat;
        noteControl.TargetEndBeat = recordedNote.EndBeat;
        // Draw already a portion of the note
        noteControl.LifeTimeInSeconds = Time.deltaTime;
        noteControl.EndBeat = recordedNote.StartBeat + (noteControl.LifeTimeInSeconds * beatsPerSecond);

        noteControl.MidiNote = midiNote;

        Label label = noteControl.Label;
        if (showPitchOfNotes)
        {
            string pitchName = MidiUtils.GetAbsoluteName(midiNote);
            label.text = " (" + pitchName + ")";
        }
        else
        {
            label.text = "";
        }

        recordedNoteEntryContainer.Add(visualElement);
        UpdateNotePosition(visualElement, midiNote, noteControl.StartBeat, noteControl.EndBeat);

        recordedNoteToRecordedNoteControlsMap.AddInsideList(recordedNote, noteControl);
    }

    public void CreatePerfectSentenceEffect()
    {
        for (int i = 0; i < 50; i++)
        {
            CreatePerfectSentenceStar();
        }
    }

    protected void CreatePerfectSentenceStar()
    {
        VisualElement star = perfectEffectStarUi.CloneTree().Children().First();
        star.style.position = new StyleEnum<Position>(Position.Absolute);
        effectsContainer.Add(star);

        StarParticleControl starControl = new StarParticleControl();
        injector.WithRootVisualElement(star).Inject(starControl);

        float xPercent = UnityEngine.Random.Range(0f, 100f);
        float yPercent = UnityEngine.Random.Range(0f, 100f);
        Vector2 startPos = new Vector2(xPercent, yPercent);
        starControl.SetPosition(startPos);
        LeanTween.value(singSceneControl.gameObject, startPos, startPos + GetRandomVector2(-50, 50), 1f)
            .setOnUpdate((Vector2 p) => starControl.SetPosition(p));

        Vector2 startScale = Vector2.one * UnityEngine.Random.Range(0.2f, 0.5f);
        starControl.SetScale(Vector2.zero);
        LeanTween.value(singSceneControl.gameObject, startScale, startScale * UnityEngine.Random.Range(1f, 2f), 1f)
            .setOnUpdate((Vector2 s) => starControl.SetScale(s))
            .setOnComplete(() => RemoveStarControl(starControl));

        starControls.Add(starControl);
    }

    private void RemoveStarControl(StarParticleControl starControl)
    {
        starControl.VisualElement.RemoveFromHierarchy();
        starControls.Remove(starControl);
    }

    public void CreatePerfectNoteEffect(Note perfectNote)
    {
        if (noteToTargetNoteControl.TryGetValue(perfectNote, out TargetNoteControl targetNoteControl))
        {
            targetNoteControl.CreatePerfectNoteEffect();
        }
    }

    private Vector2 GetRandomVector2(float min, float max)
    {
        return new Vector3(UnityEngine.Random.Range(min, max), UnityEngine.Random.Range(min, max));
    }

    protected virtual int CalculateNoteRow(int midiNote)
    {
        // Map midiNote to range of noteRows (wrap around).
        int wrappedMidiNote = midiNote;
        while (wrappedMidiNote > maxNoteRowMidiNote && wrappedMidiNote > 0)
        {
            // Reduce by one octave.
            wrappedMidiNote -= 12;
        }
        while (wrappedMidiNote < minNoteRowMidiNote && wrappedMidiNote < 127)
        {
            // Increase by one octave.
            wrappedMidiNote += 12;
        }
        // Calculate offset, such that the average note will be on the middle row
        // (thus, middle row has offset of zero).
        int offset = wrappedMidiNote - avgMidiNote;
        int noteRow = (noteRowCount / 2) + offset;
        noteRow = NumberUtils.Limit(noteRow, 0, noteRowCount - 1);
        return noteRow;
    }

    public Vector2 GetYStartAndEndInPercentForMidiNote(int midiNote)
    {
        int noteRow = CalculateNoteRow(midiNote);
        float y = noteRowToYPercent[noteRow];
        float yStart = y - noteHeightPercent;
        float yEnd = y + noteHeightPercent;
        return new Vector2(yStart, yEnd);
    }

    protected void UpdateRecordedNoteControlEndBeat(RecordedNoteControl recordedNoteControl)
    {
        recordedNoteControl.EndBeat = recordedNoteControl.StartBeat + (recordedNoteControl.LifeTimeInSeconds * beatsPerSecond);
        if (recordedNoteControl.EndBeat > recordedNoteControl.TargetEndBeat)
        {
            recordedNoteControl.EndBeat = recordedNoteControl.TargetEndBeat;
        }
    }

    protected virtual void RemoveTargetNote(TargetNoteControl targetNoteControl)
    {
        noteToTargetNoteControl.Remove(targetNoteControl.Note);
        targetNoteControl.Dispose();
    }

    protected virtual void RemoveRecordedNote(RecordedNoteControl recordedNoteControl)
    {
        recordedNoteToRecordedNoteControlsMap.Remove(recordedNoteControl.RecordedNote);
        recordedNoteControl.Dispose();
    }

    protected void RemoveTargetNotes()
    {
        noteToTargetNoteControl.Values
            .ToList()
            .ForEach(targetNoteControl => RemoveTargetNote(targetNoteControl));
    }

    protected void RemoveRecordedNotes()
    {
        recordedNoteToRecordedNoteControlsMap.Values
            .ToList()
            .ForEach(recordedNoteControls => recordedNoteControls
                .ForEach(recordedNoteControl => RemoveRecordedNote(recordedNoteControl)));
    }
}
