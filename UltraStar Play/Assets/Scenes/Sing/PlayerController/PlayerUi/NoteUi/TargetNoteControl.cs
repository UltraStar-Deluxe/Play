using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
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

    [Inject(UxmlName = R.UxmlNames.targetNote)]
    private VisualElement targetNote;

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

    [Inject(UxmlName = R.UxmlNames.effectsContainer)]
    private VisualElement effectsContainer;

    private readonly List<StarParticleControl> starControls = new();

    public void OnInjectionFinished()
    {
        targetNote.ShowByDisplay();
        recordedNote.HideByDisplay();

        if (Note.IsGolden)
        {
            image.AddToClassList("goldenNote");
        }

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


        image.style.unityBackgroundImageTintColor = finalColor;
        image.style.borderTopColor = finalColor;
        image.style.borderBottomColor = finalColor;
        image.style.borderLeftColor = finalColor;
        image.style.borderRightColor = finalColor;
    }

    private void CreateGoldenNoteEffect()
    {
        // Create several particles. Longer notes require more particles because they have more space to fill.
        int targetStarCount = Mathf.Max(6, (int)VisualElement.contentRect.width / 5);
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

        StarParticleControl starControl = injector
            .WithRootVisualElement(star)
            .CreateAndInject<StarParticleControl>();
        starControl.VisualElementToFollow = VisualElement;

        float noteWidth = VisualElement.style.width.value.value;
        float noteHeight = VisualElement.style.height.value.value;
        float xPercent = VisualElement.style.left.value.value + Random.Range(0, noteWidth);
        float yPercent = VisualElement.style.top.value.value + Random.Range(noteHeight * 0.9f, 0);
        Vector2 pos = new(xPercent, yPercent);
        starControl.SetPosition(pos);
        starControl.Rotation = Random.Range(0, 360);

        Vector2 startScale = Vector2.zero;
        starControl.SetScale(startScale);

        // Rotate a little bit
        starControl.RotationVelocityInDegreesPerSecond = 60;

        // Animate to full size, stay there a while, then remove.
        LeanTween.value(singSceneControl.gameObject, startScale, Vector2.one * Random.Range(0.25f, 0.5f), Random.Range(0.5f, 1f))
            .setOnUpdate((Vector2 s) => starControl.SetScale(s))
            .setOnComplete(() =>
            {
                LeanTween.value(singSceneControl.gameObject, 0, 0, Random.Range(1f, 4f))
                    .setOnComplete(() => RemoveStarControl(starControl));
            });

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

        StarParticleControl starControl = injector
            .WithRootVisualElement(star)
            .CreateAndInject<StarParticleControl>();
        starControl.VisualElementToFollow = VisualElement;

        star.style.marginLeft = -25;
        float xPercent = VisualElement.style.left.value.value + VisualElement.style.width.value.value;
        float yPercent = VisualElement.style.top.value.value - VisualElement.style.height.value.value / 4f;
        Vector2 pos = new(xPercent, yPercent);
        starControl.SetPosition(pos);
        starControl.Rotation = Random.Range(0, 360);

        Vector2 startScale = Vector2.zero;
        Vector2 endScale = Vector2.one * 0.8f;
        starControl.SetScale(startScale);

        // Rotate a little bit
        starControl.RotationVelocityInDegreesPerSecond = 60;

        // Animate to full size, then animate to zero size, then remove.
        float animationTime = 1.5f;
        LeanTween.value(singSceneControl.gameObject, startScale, endScale, animationTime / 2f)
            .setEaseOutSine()
            .setOnUpdate((Vector2 s) => starControl.SetScale(s))
            .setOnComplete(() =>
            {
                LeanTween.value(singSceneControl.gameObject, endScale, startScale, animationTime / 2f)
                    .setEaseOutSine()
                    .setOnUpdate((Vector2 s) => starControl.SetScale(s))
                    .setOnComplete(() => RemoveStarControl(starControl));
            });

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
