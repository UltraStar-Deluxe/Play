using CommonOnlineMultiplayer;
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

    [Inject(UxmlName = R.UxmlNames.targetNoteLyricsContainer)]
    protected VisualElement targetNoteLyricsContainer;

    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(UxmlName = R.UxmlNames.targetNote)]
    private VisualElement targetNote;

    [Inject(UxmlName = R.UxmlNames.recordedNote)]
    private VisualElement recordedNote;

    [Inject(UxmlName = R.UxmlNames.targetNoteImage)]
    private VisualElement image;

    [Inject(UxmlName = R.UxmlNames.targetNoteBorder)]
    private VisualElement targetNoteBorder;

    [Inject(Optional = true)]
    private MicProfile micProfile;

    [Inject]
    private PlayerProfile playerProfile;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private Injector injector;

    [Inject(UxmlName = R.UxmlNames.effectsContainer)]
    private VisualElement effectsContainer;

    public void OnInjectionFinished()
    {
        targetNote.ShowByDisplay();
        recordedNote.HideByDisplay();

        targetNoteLyricsContainer.Add(Label);
        Label.HideByVisibility();
        // new AutoFitLabelControl(Label, 4, 14);
        VisualElement.RegisterCallback<GeometryChangedEvent>(_ => UpdateLabelPosition());

        if (Note.IsGolden)
        {
            VisualElement.AddToClassList("goldenNote");
            VisualElement.RegisterHasGeometryCallbackOneShot(_ => CreateGoldenNoteParticleEffect());
        }

        SetStyleByMicProfile();

        // hide freestyle notes
        if (Note.Type is ENoteType.Freestyle)
        {
            VisualElement.HideByVisibility();
        }
    }

    private void SetStyleByMicProfile()
    {
        Color32 color = Note.IsGolden
            ? themeManager.GetGoldenColor()
            : GetColorForPlayer();

        image.style.unityBackgroundImageTintColor = new StyleColor(color);
        image.SetBorderColor(color);
        targetNoteBorder.SetBorderColor(color);
    }

    private Color32 GetColorForPlayer()
    {
        if (micProfile != null)
        {
            return micProfile.Color;
        }

        if (playerProfile is LobbyMemberPlayerProfile lobbyMemberPlayerProfile)
        {
            return ColorGenerationUtils.FromString(lobbyMemberPlayerProfile.Name);
        }

        return Colors.white;
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
        Label.RemoveFromHierarchy();
        VisualElement.RemoveFromHierarchy();
    }

    public void Update()
    {
        // Nothing to do
    }

    public void UpdateLabelFontSize()
    {
        if (!Label.IsVisibleByDisplay()
            || Label.text.IsNullOrEmpty()
            || !Label.IsVisibleByVisibility())
        {
            return;
        }

        AutoFitLabelControl.SetBestFitFontSize(Label, 4, 14, 20);
    }

    public void UpdateLabelPosition()
    {
        if (!Label.IsVisibleByDisplay()
            || Label.text.IsNullOrEmpty())
        {
            return;
        }

        Label.SetVisibleByVisibility(VisualElement.IsVisibleByDisplay() && VisualElementUtils.HasGeometry(VisualElement));
        if (!Label.IsVisibleByVisibility())
        {
            return;
        }

        // The note label is in another VisualElement to be fully visible (not truncated by the parent).
        // Thus, its position needs to be updated manually.
        Label.style.position = new StyleEnum<Position>(Position.Absolute);
        Rect visualElementWorldRect = VisualElement.worldBound;
        Rect targetNoteLabelWorldRect = VisualElementUtils.WorldBoundToLocalBound(Label, visualElementWorldRect);
        float preferredTextHeight = Label.GetPreferredTextSize().y;
        float labelHeight = Mathf.Max(preferredTextHeight, Label.worldBound.height);
        Label.style.top = targetNoteLabelWorldRect.yMin - labelHeight;
        Label.style.left = targetNoteLabelWorldRect.xMin;
    }
}
