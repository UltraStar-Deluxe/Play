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

    public void OnInjectionFinished()
    {
        targetNote.ShowByDisplay();
        recordedNote.HideByDisplay();

        if (Note.IsGolden)
        {
            VisualElement.AddToClassList("goldenNote");
            VisualElement.RegisterHasGeometryCallbackOneShot(_ => CreateGoldenNoteParticleEffect());
        }

        SetStyleByMicProfile();
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

    private void CreateGoldenNoteParticleEffect()
    {
        int maxParticles = 1 + (int)(VisualElement.worldBound.width / 8f);
        VfxManager.CreateParticleEffect(new ParticleEffectConfig()
        {
            particleEffect = EParticleEffect.GoldenNoteEffect,
            panelPos = VisualElement.worldBound.center,
            loop = true,
            scale = 0.2f,
            target = VisualElement,
            destroyWithTarget = true,
            moveWithTargetPanelPosProducer = () => VisualElement.worldBound.center,
            scaleBoxShapeWithTargetFactor = 0.02f,
            // A large note needs more particles
            maxParticles = maxParticles ,
            rateOverTime = 1 + maxParticles / 2,
        });
    }

    public void CreatePerfectNoteEffect()
    {
        VfxManager.CreateParticleEffect(new ParticleEffectConfig()
        {
            particleEffect = EParticleEffect.FireworksEffect2D_SingleYellowStar,
            panelPos = new Vector2(VisualElement.worldBound.xMax, VisualElement.worldBound.yMin),
            scale = 0.1f,
            target = VisualElement,
            destroyWithTarget = true,
            moveWithTargetPanelPosProducer = () => new Vector2(VisualElement.worldBound.xMax, VisualElement.worldBound.yMin),
        });
    }

    public void Dispose()
    {
        VisualElement.RemoveFromHierarchy();
    }

    public void Update()
    {
        // Nothing to do
    }
}
