using System;
using System.Collections.Generic;
using System.Linq;
using CommonOnlineMultiplayer;
using UniInject;
using UniInject.Extensions;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public abstract class AbstractSingSceneNoteDisplayer : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    protected VisualElement rootVisualElement;

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
    protected GameObject gameObject;

    [Inject]
    protected Injector injector;

    [Inject]
    protected SingSceneMedleyControl medleyControl;

    [Inject]
    protected PlayerProfile playerProfile;

    [Inject(Optional = true)]
    protected MicProfile micProfile;

    protected LineDisplayer lineDisplayer;

    protected double beatsPerSecond;

    protected readonly List<TargetNoteControl> targetNoteControls = new();
    protected readonly Dictionary<Note, TargetNoteControl> noteToTargetNoteControl = new();
    protected readonly Dictionary<RecordedNote, List<RecordedNoteControl>> recordedNoteToRecordedNoteControlsMap = new();

    protected readonly List<StarParticleControl> starControls = new();

    protected int avgMidiNote;

    // The number of rows on which notes can be placed.
    protected int noteRowCount;
    protected Dictionary<int, float> noteRowToYPercent;

    protected int maxNoteRowMidiNote;
    protected int minNoteRowMidiNote;
    protected float noteHeightPercent;

    // Only for debugging
    private bool displayRoundedAndActualRecordedNotes;
    private bool showPitchOfNotes;

    private int fadeOutAnimationId;
    private readonly List<int> fadeOutLyricsOnNotesAnimationIds = new();
    private readonly ReactiveProperty<float> lyricsOnNotesOpacity = new(1);

    private readonly HashSet<Label> initializedNoteLabelWidth = new();

    private readonly Subject<TargetNoteControlCreatedEvent> targetNoteControlCreatedEventStream = new();
    public IObservable<TargetNoteControlCreatedEvent> TargetNoteControlCreatedEventStream => targetNoteControlCreatedEventStream;

    private readonly Subject<RecordedNoteControlCreatedEvent> recordedNoteControlCreatedEventStream = new();
    public IObservable<RecordedNoteControlCreatedEvent> RecordedNoteControlCreatedEventStream => recordedNoteControlCreatedEventStream;

    protected abstract bool TryGetNotePositionInPercent(VisualElement visualElement, int midiNote, double noteStartBeat, double noteEndBeat, out Rect result);

    protected void UpdateNotePosition(VisualElement visualElement, int midiNote, double noteStartBeat, double noteEndBeat)
    {
        if (!TryGetNotePositionInPercent(visualElement, midiNote, noteStartBeat, noteEndBeat, out Rect notePositionInPercent))
        {
            return;
        }

        visualElement.style.position = new StyleEnum<Position>(Position.Absolute);
        visualElement.style.width = new StyleLength(new Length(notePositionInPercent.width, LengthUnit.Percent));
        visualElement.style.height = new StyleLength(new Length(notePositionInPercent.height, LengthUnit.Percent));
        visualElement.style.left = new StyleLength(new Length(notePositionInPercent.xMin, LengthUnit.Percent));
        visualElement.style.top = new StyleLength(new Length(notePositionInPercent.yMin, LengthUnit.Percent));
    }

    public virtual void OnInjectionFinished()
    {
        targetNoteEntryContainer.Clear();
        recordedNoteEntryContainer.Clear();
        effectsContainer.Clear();

        beatsPerSecond = SongMetaBpmUtils.BeatsPerSecond(songMeta);
        playerNoteRecorder.RecordedNoteStartedEventStream.Subscribe(recordedNoteStartedEvent =>
        {
            DisplayRecordedNote(recordedNoteStartedEvent.RecordedNote);
        });
        playerNoteRecorder.RecordedNoteContinuedEventStream.Subscribe(recordedNoteContinuedEvent =>
        {
            DisplayRecordedNote(recordedNoteContinuedEvent.RecordedNote);
        });

        lineDisplayer = new LineDisplayer();
        lineDisplayer.LineColor = Color.grey;
        injector.Inject(lineDisplayer);

        lyricsOnNotesOpacity.Subscribe(newValue =>
        {
            targetNoteControls.ForEach(targetNoteControl => targetNoteControl.Label.style.opacity = newValue);
        });
    }

    public virtual void Update()
    {
        // Update notes
        for (int i = 0; i < targetNoteControls.Count; i++)
        {
            TargetNoteControl targetNoteControl = targetNoteControls[i];
            UpdateTargetNoteControl(targetNoteControl, i);
        }
        recordedNoteToRecordedNoteControlsMap.Values
            .ForEach(recordedNoteControls => recordedNoteControls
                .ForEach(recordedNoteControl => UpdateRecordedNoteControl(recordedNoteControl)));

        // Update stars
        starControls.ForEach(starControl => starControl.Update());
    }

    protected abstract void UpdateTargetNoteControl(TargetNoteControl targetNoteControl, int indexInList);

    protected void UpdateTargetNoteLabelWith(TargetNoteControl targetNoteControl, int indexInList)
    {
        if (initializedNoteLabelWidth.Contains(targetNoteControl.Label))
        {
            return;
        }

        TargetNoteControl nextTargetNoteControl = targetNoteControls.ElementAtOrDefault(indexInList + 1);
        if (targetNoteControl.Label.IsVisibleByDisplay()
            && nextTargetNoteControl != null
            && targetNoteControl.Note.MidiNote == nextTargetNoteControl.Note.MidiNote)
        {
            initializedNoteLabelWidth.Add(targetNoteControl.Label);

            // Width of label until start of following note
            if (!TryGetNotePositionInPercent(targetNoteControl.Label, 60, targetNoteControl.Note.StartBeat, nextTargetNoteControl.Note.StartBeat, out Rect notePositionInPercent))
            {
                return;
            }
            targetNoteControl.Label.style.width = Length.Percent(notePositionInPercent.width);
            targetNoteControl.Label.RegisterHasGeometryCallbackOneShot(_ => targetNoteControl.UpdateLabelFontSize());
        }
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

    public virtual void SetLineCount(int lineCount)
    {
        if (lineDisplayer == null)
        {
            return;
        }

        noteRowCount = GetNoteRowCount(lineCount);
        noteRowToYPercent = new Dictionary<int, float>();

        float lineHeightPercent = 1.0f / lineCount;
        noteHeightPercent = lineHeightPercent / 2.0f;

        float marginTopPercent = noteHeightPercent;
        float marginBottomPercent = noteHeightPercent;
        float availableHeightPercent = 1 - marginTopPercent - marginBottomPercent;
        for (int i = 0; i < noteRowCount; i++)
        {
            noteRowToYPercent[i] = 1 - (marginTopPercent + (availableHeightPercent * (float)i / noteRowCount));
        }

        // Draw every second line.
        List<float> yPercentagesToDrawLinesFor = new();
        for (int i = 0; i < noteRowToYPercent.Count; i++)
        {
            if (i % 2 == 0)
            {
                yPercentagesToDrawLinesFor.Add(noteRowToYPercent[i]);
            }
        }
        lineDisplayer.DrawLines(yPercentagesToDrawLinesFor.ToArray());
    }

    public static int GetNoteRowCount(int lineCount)
    {
        // Notes can be placed on and between the drawn lines (between causes -1).
        // The first and last line is not used (which causes -2). Thus, in total it is -3.
        int result = (lineCount * 2) - 3;
        // Check that there is at least one row for every possible note in an octave.
        return NumberUtils.Limit(result, 12, int.MaxValue);
    }

    public static int GetLineCount(int noteRowCount)
    {
        return (int)Math.Ceiling((noteRowCount + 3) / 2.0);
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

        TargetNoteControl targetNoteControl = injector
            .WithBindingForInstance(note)
            .WithRootVisualElement(visualElement)
            .CreateAndInject<TargetNoteControl>();

        Label label = targetNoteControl.Label;
        string pitchName = MidiUtils.GetAbsoluteName(note.MidiNote);
        if (settings.ShowLyricsOnNotes && showPitchOfNotes)
        {
            label.text = GetDisplayText(note) + " (" + pitchName + ")";
        }
        else if (settings.ShowLyricsOnNotes)
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
            label.HideByDisplay();
        }
        label.style.opacity = lyricsOnNotesOpacity.Value;

        targetNoteEntryContainer.Add(visualElement);
        UpdateTargetNoteControl(targetNoteControl, -1);

        noteToTargetNoteControl[note] = targetNoteControl;
        targetNoteControls.Add(targetNoteControl);

        try
        {
            targetNoteControlCreatedEventStream.OnNext(new TargetNoteControlCreatedEvent(targetNoteControl));
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

        return targetNoteControl;
    }

    public string GetDisplayText(Note note)
    {
        // Show underscore as space.
        // Underscore is used in song editor to show notes with missing lyrics after speech recognition.
        string displayText = note.Text.Replace("_", " ");
        switch (note.Type)
        {
            case ENoteType.Freestyle:
                return $"<i><b><color=#c00000>{displayText}</color></b></i>";
            case ENoteType.Golden:
                return $"<b>{displayText}</b>";
            case ENoteType.Rap:
            case ENoteType.RapGolden:
                return $"<i><b><color=#ffa500ff>{displayText}</color></b></i>";
            default:
                return displayText;
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

        RecordedNoteControl recordedNoteControl = new();
        childInjector.Inject(recordedNoteControl);

        recordedNoteControl.StartBeat = recordedNote.StartBeat;
        recordedNoteControl.TargetEndBeat = recordedNote.EndBeat;
        // Draw already a portion of the note
        recordedNoteControl.LifeTimeInSeconds = Time.deltaTime;
        recordedNoteControl.EndBeat = recordedNote.StartBeat + (recordedNoteControl.LifeTimeInSeconds * beatsPerSecond);

        recordedNoteControl.MidiNote = midiNote;
        recordedNoteControl.Color = CommonOnlineMultiplayerUtils.GetPlayerColor(playerProfile, micProfile);

        Label label = recordedNoteControl.Label;
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
        UpdateNotePosition(visualElement, midiNote, recordedNoteControl.StartBeat, recordedNoteControl.EndBeat);

        recordedNoteToRecordedNoteControlsMap.AddInsideList(recordedNote, recordedNoteControl);

        try
        {
            recordedNoteControlCreatedEventStream.OnNext(new RecordedNoteControlCreatedEvent(recordedNoteControl));
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }

    protected void CreatePerfectSentenceStar()
    {
        VisualElement star = perfectEffectStarUi.CloneTree().Children().First();
        star.style.position = new StyleEnum<Position>(Position.Absolute);
        effectsContainer.Add(star);

        StarParticleControl starControl = injector
            .WithRootVisualElement(star)
            .CreateAndInject<StarParticleControl>();

        float xPercent = Random.Range(0f, 100f);
        float yPercent = Random.Range(0f, 100f);
        Vector2 startPos = new(xPercent, yPercent);
        starControl.SetPosition(startPos);
        LeanTween.value(singSceneControl.gameObject, startPos, startPos + GetRandomVector2(-50, 50), 1f)
            .setOnUpdate((Vector2 p) => starControl.SetPosition(p));

        Vector2 startScale = Vector2.one * Random.Range(0.2f, 0.5f);
        starControl.SetScale(Vector2.zero);
        LeanTween.value(singSceneControl.gameObject, startScale, startScale * Random.Range(1f, 2f), 1f)
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
        return new Vector2(Random.Range(min, max), Random.Range(min, max));
    }

    protected virtual int CalculateNoteRow(int midiNote, int beat)
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

    public Vector2 GetYStartAndEndInPercentForMidiNote(int midiNote, int beat)
    {
        int rawNoteRow = CalculateNoteRow(midiNote, beat);
        int noteRow = NumberUtils.ModNegativeToPositive(rawNoteRow, noteRowCount);
        if (!noteRowToYPercent.TryGetValue(noteRow, out float y))
        {
            Debug.LogWarning($"No vertical position for note row at index {noteRow} (midiNote {midiNote}, beat {beat})");
            y = noteHeightPercent;
        }
        float yStart = y - noteHeightPercent;
        float yEnd = y + noteHeightPercent;
        return new Vector2(yStart, yEnd);
    }

    public virtual float GetXInPercent(double positionInMillis)
    {
        return 0;
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
        targetNoteControls.Remove(targetNoteControl);
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

    public void FadeOut(float animTimeInSeconds)
    {
        LeanTween.cancel(fadeOutAnimationId);
        fadeOutAnimationId = AnimationUtils.FadeOutVisualElement(gameObject, rootVisualElement, animTimeInSeconds);
    }

    public void FadeIn(float animTimeInSeconds)
    {
        LeanTween.cancel(fadeOutAnimationId);
        fadeOutAnimationId = AnimationUtils.FadeInVisualElement(gameObject, rootVisualElement, animTimeInSeconds);
    }

    public void FadeOutLyricsOnNotes(float animTimeInSeconds)
    {
        LeanTweenUtils.CancelAndClear(fadeOutLyricsOnNotesAnimationIds);
        fadeOutLyricsOnNotesAnimationIds.Add(LeanTween
            .value(gameObject, lyricsOnNotesOpacity.Value, 0, animTimeInSeconds)
            .setOnUpdate(interpolatedValue => lyricsOnNotesOpacity.Value = interpolatedValue)
            .id);
    }

    public void FadeInLyricsOnNotes(float animTimeInSeconds)
    {
        LeanTweenUtils.CancelAndClear(fadeOutLyricsOnNotesAnimationIds);
        fadeOutLyricsOnNotesAnimationIds.Add(LeanTween
            .value(gameObject, lyricsOnNotesOpacity.Value, 1, animTimeInSeconds)
            .setOnUpdate(interpolatedValue => lyricsOnNotesOpacity.Value = interpolatedValue)
            .id);
    }
}
