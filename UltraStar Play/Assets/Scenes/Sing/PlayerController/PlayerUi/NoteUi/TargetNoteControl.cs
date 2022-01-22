using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UniInject;
using UnityEngine.UIElements;

public class TargetNoteControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(goldenNoteStarUi))]
    protected VisualTreeAsset goldenNoteStarUi;

    [Inject(Key = nameof(perfectEffectStarUi))]
    protected VisualTreeAsset perfectEffectStarUi;

    [Inject]
    public Note Note { get; private set; }

    [Inject(UxmlName = R.UxmlNames.targetNoteLabel)]
    public Label Label { get; private set; }

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(UxmlName = R.UxmlNames.recordedNote)]
    private VisualElement recordedNote;

    [Inject(UxmlName = R.UxmlNames.targetNoteImage)]
    private VisualElement image;

    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private Injector injector;

    protected VisualElement effectsContainer;

    private readonly List<StarParticleControl> starControls = new List<StarParticleControl>();

    public TargetNoteControl(VisualElement effectsContainer)
    {
        this.effectsContainer = effectsContainer;
    }

    public void OnInjectionFinished()
    {
        recordedNote.HideByDisplay();

        SetStyleByMicProfile();
    }

    public void Update()
    {
        if (Note.IsGolden)
        {
            CreateGoldenNoteEffect();
        }

        starControls.ForEach(starControl => starControl.Update());
    }

    private void SetStyleByMicProfile()
    {
        if (micProfile == null)
        {
            return;
        }

        Color color = micProfile.Color;

        // Make freestyle and rap notes transparent
        Color finalColor = Note.Type is ENoteType.Freestyle or ENoteType.Rap or ENoteType.RapGolden
            ? color.WithAlpha(0.3f)
            : color;

        image.style.unityBackgroundImageTintColor = new StyleColor(finalColor);
    }

    private void CreateGoldenNoteEffect()
    {
        // Create several particles. Longer notes require more particles because they have more space to fill.
        int targetStarCount = Mathf.Max(6, (int)VisualElement.contentRect.width / 10);
        if (starControls.Count < targetStarCount)
        {
            CreateGoldenStar();
        }
    }

    private void CreateGoldenStar()
    {
        VisualElement star = goldenNoteStarUi.CloneTree().Children().First();
        star.style.position = new StyleEnum<Position>(Position.Absolute);
        effectsContainer.Add(star);

        StarParticleControl starControl = new StarParticleControl();
        starControl.VisualElementToFollow = VisualElement;
        injector.WithRootVisualElement(star).Inject(starControl);

        float noteWidth = VisualElement.style.width.value.value;
        float noteHeight = VisualElement.style.height.value.value;
        float xPercent = VisualElement.style.left.value.value + Random.Range(0, noteWidth);
        float yPercent = VisualElement.style.bottom.value.value + Random.Range(-noteHeight / 2, noteHeight / 2);
        Vector2 pos = new Vector2(xPercent, yPercent);
        starControl.SetPosition(pos);
        starControl.Rotation = Random.Range(0, 360);

        Vector2 startScale = Vector2.zero;
        starControl.SetScale(startScale);

        LeanTween.value(singSceneControl.gameObject, startScale, Vector2.one * Random.Range(0.5f, 1f), Random.Range(1f, 2f))
            .setOnUpdate((Vector2 s) => starControl.SetScale(s))
            .setOnComplete(() => RemoveStarControl(starControl));

        starControls.Add(starControl);
    }

    public void CreatePerfectNoteEffect()
    {
        CreatePerfectStar();
    }

    private void CreatePerfectStar()
    {
        VisualElement star = perfectEffectStarUi.CloneTree().Children().First();
        star.style.position = new StyleEnum<Position>(Position.Absolute);
        effectsContainer.Add(star);

        StarParticleControl starControl = new StarParticleControl();
        starControl.VisualElementToFollow = VisualElement;
        injector.WithRootVisualElement(star).Inject(starControl);

        star.style.marginLeft = -25;
        float xPercent = VisualElement.style.left.value.value + VisualElement.style.width.value.value;
        float yPercent = VisualElement.style.bottom.value.value;
        Vector2 pos = new Vector2(xPercent, yPercent);
        starControl.SetPosition(pos);
        starControl.Rotation = Random.Range(0, 360);

        Vector2 startScale = Vector2.one * 0.5f;
        starControl.SetScale(startScale);
        LeanTween.value(singSceneControl.gameObject, startScale, Vector2.one, 0.6f)
            .setOnUpdate((Vector2 s) => starControl.SetScale(s))
            .setOnComplete(() => RemoveStarControl(starControl));

        starControls.Add(starControl);
    }

    private void RemoveStarControl(StarParticleControl starControl)
    {
        starControl.VisualElement.RemoveFromHierarchy();
        starControls.Remove(starControl);
    }

    public void Dispose()
    {
        VisualElement.RemoveFromHierarchy();
        starControls.ToList().ForEach(starControl => RemoveStarControl(starControl));
    }
}
