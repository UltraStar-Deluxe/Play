using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class RecordedNoteControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(goldenNoteHitStarUi))]
    protected VisualTreeAsset goldenNoteHitStarUi;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(UxmlName = R.UxmlNames.targetNote)]
    private VisualElement targetNoteVisualElement;

    [Inject(UxmlName = R.UxmlNames.recordedNote)]
    private VisualElement recordedNoteVisualElement;

    [Inject(UxmlName = R.UxmlNames.recordedNoteImage)]
    private VisualElement image;

    [Inject(UxmlName = R.UxmlNames.recordedNoteLabel)]
    public Label Label { get; private set; }

    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject]
    public RecordedNote RecordedNote { get; private set; }

    [Inject(UxmlName = R.UxmlNames.effectsContainer)]
    private VisualElement effectsContainer;

    [Inject]
    private Injector injector;

    [Inject]
    private GameObject gameObject;

    private readonly List<StarParticleControl> starParticleControls = new();

    private float lastNoteWidth;

    public int MidiNote { get; set; }

    // The end beat is a double here, in contrast to the RecordedNote.
    // This is because the UiRecordedNote is drawn smoothly from start to end of the RecordedNote using multiple frames.
    // Therefor, the resolution of start and end for UiRecordedNotes must be more fine grained than whole beats.
    public int StartBeat { get; set; }
    public double EndBeat { get; set; }
    public int TargetEndBeat { get; set; }
    public float LifeTimeInSeconds { get; set; }

    public void OnInjectionFinished()
    {
        targetNoteVisualElement.HideByDisplay();
        recordedNoteVisualElement.ShowByDisplay();

        if (micProfile != null)
        {
            SetStyleByMicProfile(micProfile);
        }
    }

    public void Update()
    {
        LifeTimeInSeconds += Time.deltaTime;

        starParticleControls.ForEach(starParticleControl => starParticleControl.Update());

        if (EndBeat < TargetEndBeat
            && MidiNote == RecordedNote.TargetNote.MidiNote
            && RecordedNote.TargetNote.IsGolden)
        {
            CreateGoldenNoteHitEffect();
        }

        lastNoteWidth = VisualElement.style.width.value.value;
    }

    private void CreateGoldenNoteHitEffect()
    {
        for (int i = 0; i < 3; i++)
        {
            CreateGoldenNoteHitStar();
        }
    }

    private void CreateGoldenNoteHitStar()
    {
        VisualElement star = goldenNoteHitStarUi.CloneTree().Children().First();
        star.style.position = new StyleEnum<Position>(Position.Absolute);
        effectsContainer.Add(star);

        StarParticleControl starControl = injector
            .WithRootVisualElement(star)
            .CreateAndInject<StarParticleControl>();
        starControl.VisualElementToFollow = VisualElement;

        float noteWidth = VisualElement.style.width.value.value;
        float noteHeight = VisualElement.style.height.value.value;
        // Place at right side of recorded note.
        float xPercent = VisualElement.style.left.value.value + Random.Range(lastNoteWidth, noteWidth);
        float yPercent = VisualElement.style.top.value.value + Random.Range(noteHeight * 0.9f, 0);
        Vector2 startPos = new(xPercent, yPercent);
        starControl.SetPosition(startPos);
        starControl.Rotation = Random.Range(0, 360);

        float startSize = Random.Range(0.4f, 0.5f);
        Vector2 startScale = new(startSize, startSize);
        starControl.SetScale(startScale);

        // Move and rotate a little bit
        starControl.RotationVelocityInDegreesPerSecond = 60;
        starControl.VelocityInPercentPerSecond = new Vector2(Random.Range(-3, 0), Random.Range(-10, 10));

        // Shrink size and fade out then remove
        LeanTween.value(gameObject, 1, 0, Random.Range(0.3f, 0.7f))
            .setOnUpdate(factor =>
            {
                starControl.SetScale(startScale * factor);
                starControl.SetOpacity(factor);
            })
            .setOnComplete(() =>
            {
                RemoveStarControl(starControl);
            });

        starParticleControls.Add(starControl);
    }

    private void RemoveStarControl(StarParticleControl starParticleControl)
    {
        starParticleControl.VisualElement.RemoveFromHierarchy();
        starParticleControls.Remove(starParticleControl);
    }

    private void SetStyleByMicProfile(MicProfile micProfile)
    {
        // If no target note, then remove saturation from color and make transparent
        Color color = micProfile.Color;
        Color finalColor = (RecordedNote != null && RecordedNote.TargetNote == null)
            ? color.RgbToHsv().WithGreen(0).HsvToRgb().WithAlpha(0.25f)
            : color;
        image.style.unityBackgroundImageTintColor = finalColor;
        image.style.backgroundColor = finalColor;
    }

    public void Dispose()
    {
        VisualElement.RemoveFromHierarchy();
        starParticleControls.ToList().ForEach(starParticleControl => RemoveStarControl(starParticleControl));
    }
}
