using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using Random = UnityEngine.Random;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class LetterCollectorGameControl : MonoBehaviour, INeedInjection
{
    [InjectedInInspector]
    public TextAsset creditsEntriesTextAsset;

    [InjectedInInspector]
    public VisualTreeAsset creditsEntryUi;

    [InjectedInInspector]
    public VectorImage[] regularBanners;

    [InjectedInInspector]
    public VectorImage goldBanner;

    [InjectedInInspector]
    public Sprite silverBanner;

    [InjectedInInspector]
    public bool forceFadeOut;

    [Inject(UxmlName = R.UxmlNames.player)]
    private VisualElement player;

    [Inject(UxmlName = R.UxmlNames.entriesContainer)]
    private VisualElement entriesContainer;

    [Inject(UxmlName = R.UxmlNames.scoreLabel)]
    private Label scoreLabel;

    [Inject(UxmlName = R.UxmlNames.bonusLabel)]
    private Label bonusLabel;

    [Inject(UxmlName = R.UxmlNames.background)]
    private VisualElement background;

    [Inject(UxmlName = R.UxmlNames.secondBackground)]
    private VisualElement secondBackground;

    [Inject(UxmlName = R.UxmlNames.categoryNameLabel)]
    private Label categoryNameLabel;

    [Inject(UxmlName = R.UxmlNames.categoryNameContainer)]
    private VisualElement categoryNameContainer;

    [Inject(UxmlName = R.UxmlNames.creditsSummaryLabel)]
    private Label creditsSummaryLabel;

    [Inject(UxmlName = R.UxmlNames.skipButton)]
    private Button skipButton;

    [Inject]
    private PanelHelper panelHelper;

    [Inject]
    private Injector injector;

    private int categoryIndex;
    private List<CreditsCategoryEntry> creditsCategoryEntries;
    private List<CreditsEntry> remainingCreditsEntries;
    private readonly List<CreditsEntryControl> entryControls = new();

    private int score;

    private float startTimeInSeconds;
    private float nextSpawnTimeInSeconds;
    private float spawnPauseInSeconds;

    private bool isFadeOut;
    private bool isFirstEntryControl = true;

    private Vector2 lastCreatedEntryControlPosition;

    public void Start()
    {
        // Init UI
        creditsSummaryLabel.text = "";
        scoreLabel.text = "0";
        bonusLabel.style.opacity = 0;
        entriesContainer.Clear();

        // Parse credit entries
        creditsCategoryEntries = JsonConverter.FromJson<List<CreditsCategoryEntry>>(creditsEntriesTextAsset.text);
        SelectNextCategoryEntry();

        skipButton.RegisterCallbackButtonTriggered(() => forceFadeOut = true);
    }

    private void SelectNextCategoryEntry()
    {
        if (categoryIndex >= creditsCategoryEntries.Count
            || isFadeOut)
        {
            return;
        }
        CreditsCategoryEntry categoryEntry = creditsCategoryEntries[categoryIndex];
        remainingCreditsEntries = categoryEntry.Entries.ToList();
        categoryIndex++;

        categoryNameLabel.text = categoryEntry.Name;
        categoryNameLabel.SetVisibleByVisibility(!categoryNameLabel.text.IsNullOrEmpty());
        categoryNameLabel.style.top = new StyleLength(new Length(40, LengthUnit.Percent));
        LeanTween.value(gameObject, 40, 0, 2)
            .setOnUpdate(value => categoryNameLabel.style.top = new StyleLength(new Length(value, LengthUnit.Percent)));
    }

    public void Update()
    {
        UpdatePlayerPosition();
        UpdateEntryControls();
        CollectCharacters();
        SpawnCreditEntries();
    }

    private void UpdateEntryControls()
    {
        entryControls.ToList().ForEach(it =>
        {
            it.Update();
            if (it.VisualElement.parent == null)
            {
                entryControls.Remove(it);
            }
        });

        if (!isFadeOut && forceFadeOut)
        {
            StartFadeOut();
        }

        if (!isFadeOut && entryControls.Count == 0 && remainingCreditsEntries.Count == 0)
        {
            if (categoryIndex < creditsCategoryEntries.Count)
            {
                SelectNextCategoryEntry();
            }
            else
            {
                StartFadeOut();
            }
        }
    }

    private void StartFadeOut()
    {
        isFadeOut = true;
        float fadeOutTimeInSeconds = 4f;
        LeanTween.value(gameObject, 1, 0, fadeOutTimeInSeconds)
            .setOnUpdate(value =>
            {
                player.style.opacity = value;
                categoryNameContainer.style.opacity = value;
                skipButton.style.opacity = value;
                background.style.unityBackgroundImageTintColor = new StyleColor(new Color(1, 1, 1, value));
                secondBackground.style.unityBackgroundImageTintColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1 - value));
            });

        string creditsSummaryText = creditsCategoryEntries
            .Select(category =>
            {
                string categoryContent = category.Entries
                    .Select(entry => entry.MainNameAndNickname)
                    .JoinWith("\n");
                return !category.Name.IsNullOrEmpty()
                    ? ($"<i>{category.Name}</i>" + "\n" + categoryContent)
                    : categoryContent;
            }).JoinWith("\n\n");
        // Extra line breaks for continued scroll range
        creditsSummaryLabel.text = creditsSummaryText + "\n\n\n\n\n\n\n\n";
    }

    private void SpawnCreditEntries()
    {
        if (nextSpawnTimeInSeconds <= Time.time)
        {
            spawnPauseInSeconds = Random.Range(1f, 3f);
            nextSpawnTimeInSeconds = Time.time + spawnPauseInSeconds;

            CreateNextEntryControl();
        }
    }

    private void CollectCharacters()
    {
        entryControls.SelectMany(entryControl => entryControl.CharacterControls)
            .Where(characterControl => characterControl.IsMoving && !characterControl.IsCollected && characterControl.Character != " ")
            .ForEach(characterControl =>
            {
                float playerDistance = Vector2.Distance(characterControl.VisualElement.worldBound.center, player.worldBound.center);
                float sizeSum = player.worldBound.size.magnitude + characterControl.VisualElement.worldBound.size.magnitude;
                float distanceThreshold = sizeSum * 0.5f;
                if (playerDistance < distanceThreshold)
                {
                    characterControl.IsCollected = true;
                    characterControl.VisualElement.HideByVisibility();

                    // One regular point for the character
                    score += 1;
                    scoreLabel.text = score.ToString();
                }
            });
    }

    private void UpdatePlayerPosition()
    {
        Vector2 screenSize = ApplicationUtils.GetScreenSizeInPanelCoordinates(panelHelper);
        Vector2 pointerPosition = InputUtils.GetPointerPositionInPanelCoordinates(panelHelper);
        float pointerPositionXPercent = pointerPosition.x / screenSize.x;
        player.style.left = new StyleLength(new Length(pointerPositionXPercent * 100f, LengthUnit.Percent));
    }

    private void CreateNextEntryControl()
    {
        if (remainingCreditsEntries.IsNullOrEmpty())
        {
            return;
        }

        CreateEntryControl(remainingCreditsEntries[0]);
        remainingCreditsEntries.RemoveAt(0);
    }

    private void CreateEntryControl(CreditsEntry creditsEntry)
    {
        VisualElement visualElement = creditsEntryUi.CloneTree().Children().FirstOrDefault();
        CreditsEntryControl entryControl = injector
            .WithRootVisualElement(visualElement)
            .CreateAndInject<CreditsEntryControl>();
        if (creditsEntry.Banner is "gold" or "silver")
        {
            entryControl.Banner.RemoveFromClassList("creditsRegularBanner");
            entryControl.Banner.AddToClassList("creditsGoldBanner");
            if (creditsEntry.Banner == "gold")
            {
                entryControl.Banner.style.backgroundImage = new StyleBackground(goldBanner);
            }
            else
            {
                entryControl.Banner.style.backgroundImage = new StyleBackground(silverBanner);
            }
        }
        else
        {
            entryControl.Banner.RemoveFromClassList("creditsGoldBanner");
            entryControl.Banner.AddToClassList("creditsRegularBanner");
            entryControl.Banner.style.backgroundImage = new StyleBackground(regularBanners[Random.Range(0, regularBanners.Length - 1)]);
        }

        entriesContainer.Add(visualElement);
        visualElement.RegisterCallbackOneShot<GeometryChangedEvent>(evt =>
        {
            if (isFirstEntryControl)
            {
                isFirstEntryControl = false;
                visualElement.style.top = 50;
                visualElement.style.left = 250;
                lastCreatedEntryControlPosition = new Vector2(visualElement.style.left.value.value, visualElement.style.top.value.value);
            }
            else
            {
                // Random position with some distance to last created object
                float distance = 0;
                Vector2 entryControlPosition;
                int i = 0;
                do
                {
                    visualElement.style.top = Random.Range(0, 200);
                    visualElement.style.left = Random.Range(0, 800 - visualElement.worldBound.size.x);
                    entryControlPosition = new Vector2(visualElement.style.left.value.value, visualElement.style.top.value.value);
                    distance = Vector2.Distance(lastCreatedEntryControlPosition, entryControlPosition);
                    i++;
                } while (distance < 200 && i < 100);
                lastCreatedEntryControlPosition = entryControlPosition;
            }

            // Keep fix size in pixels even if child content changes (i.e. even when the child letters are re-parented)
            visualElement.style.width = visualElement.worldBound.size.x;
            visualElement.style.height = visualElement.worldBound.size.y;
        });

        // Create main name
        string mainName = !creditsEntry.Name.IsNullOrEmpty()
            ? creditsEntry.Name
            : creditsEntry.Nickname;
        entryControl.CreateCharacterControls(mainName, entryControl.NameCharacterContainer);

        // Create nickname
        if (!creditsEntry.Nickname.IsNullOrEmpty() && creditsEntry.Nickname != mainName)
        {
            entryControl.CreateCharacterControls(creditsEntry.Nickname, entryControl.NicknameCharacterContainer);
        }

        // Add comment
        if (!creditsEntry.Comment.IsNullOrEmpty())
        {
            entryControl.CommentLabel.text = creditsEntry.Comment;
        }

        entryControls.Add(entryControl);
    }
}
