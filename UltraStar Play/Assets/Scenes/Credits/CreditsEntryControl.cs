using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class CreditsEntryControl : INeedInjection, IDisposable, IInjectionFinishedListener
{
    [Inject(Key = Injector.RootVisualElementInjectionKey)]
    public VisualElement VisualElement { get; private set; }

    [Inject(UxmlName = R.UxmlNames.nameCharacterContainer)]
    public VisualElement NameCharacterContainer { get; private set; }

    [Inject(UxmlName = R.UxmlNames.nicknameCharacterContainer)]
    public VisualElement NicknameCharacterContainer { get; private set; }

    [Inject(UxmlName = R.UxmlNames.commentLabel)]
    public Label CommentLabel { get; private set; }

    [Inject(UxmlName = R.UxmlNames.banner)]
    public VisualElement Banner { get; private set; }

    [Inject]
    private PanelHelper panelHelper;

    [Inject]
    private Injector injector;

    [Inject]
    private GameObject gameObject;

    public List<CreditsCharacterControl> CharacterControls { get; private set; } = new();

    private bool isFadingOut;

    public void OnInjectionFinished()
    {
        CommentLabel.text = "";
        NameCharacterContainer.Clear();
        NicknameCharacterContainer.Clear();

        // Fade in then start movement of characters
        float fadeInTimeInSeconds = Random.Range(0.5f, 1.5f);
        VisualElement.style.opacity = 0;
        LeanTween.value(gameObject, 0, 1, fadeInTimeInSeconds)
            .setOnUpdate(value => VisualElement.style.opacity = value)
            .setOnComplete(() =>
            {
                CharacterControls.ForEach(it => it.InitMovement());
            });
    }

    public void Update()
    {
        CharacterControls.ForEach(it => it.Update());

        // Comment label should not be larger than the banner
        CommentLabel.style.maxWidth = Banner.worldBound.width;

        // Fade out and remove when no characters visible anymore
        if (!isFadingOut)
        {
            int visibleCharacterCount = CharacterControls
                .Count(it => !it.IsCollected && it.Character != " "
                                             && it.Label.worldBound.yMax < Screen.height);
            if (visibleCharacterCount == 0)
            {
                isFadingOut = true;
                float fadeOutTimeInSeconds = Random.Range(0.2f, 0.75f);
                LeanTween.value(gameObject, 1, 0, fadeOutTimeInSeconds)
                    .setOnUpdate(value => VisualElement.style.opacity = value)
                    .setOnComplete(() => Dispose());
            }
        }
    }

    public void CreateCharacterControls(string text, VisualElement parent)
    {
        text.ForEach(c => CreateCharacterControl(c, parent));
    }

    private void CreateCharacterControl(char c, VisualElement parent)
    {
        Label label = new Label();
        label.AddToClassList("creditsEntryCharacter");
        if (c == ' ')
        {
            label.style.marginLeft = 3;
            label.style.marginRight = 3;
        }

        CreditsCharacterControl characterControl = injector
            .WithRootVisualElement(label)
            .CreateAndInject<CreditsCharacterControl>();
        characterControl.Character = c.ToString();

        parent.Add(label);

        CharacterControls.Add(characterControl);
    }

    private void RemoveCharacterControls()
    {
        CharacterControls.ForEach(it => it.Dispose());
        CharacterControls.Clear();
    }

    public void Dispose()
    {
        CharacterControls.ForEach(it => it.Dispose());
        VisualElement.RemoveFromHierarchy();
    }
}
