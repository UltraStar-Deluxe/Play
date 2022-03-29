using System;
using System.Collections.Generic;
using System.Linq;
using ProTrans;
using UniInject;
using UniInject.Extensions;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingingResultsSceneControl : MonoBehaviour, INeedInjection, IBinder, ITranslator
{
    [InjectedInInspector]
    public VisualTreeAsset nPlayerUi;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.sceneSubtitle)]
    private Label songLabel;

    [Inject(UxmlName = R.UxmlNames.onePlayerLayout)]
    public VisualElement onePlayerLayout;

    [Inject(UxmlName = R.UxmlNames.twoPlayerLayout)]
    public VisualElement twoPlayerLayout;

    [Inject(UxmlName = R.UxmlNames.nPlayerLayout)]
    public VisualElement nPlayerLayout;

    [Inject(UxmlName = R.UxmlNames.continueButton)]
    public Button continueButton;

    [Inject(UxmlName = R.UxmlNames.hiddenContinueButton)]
    public Button hiddenContinueButton;

    [Inject]
    private Statistics statistics;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    private SingingResultsSceneData sceneData;

    private List<SingingResultsPlayerControl> singingResultsPlayerUiControls = new List<SingingResultsPlayerControl>();

    public static SingingResultsSceneControl Instance
    {
        get
        {
            return FindObjectOfType<SingingResultsSceneControl>();
        }
    }

    void Start()
    {
        hiddenContinueButton.RegisterCallbackButtonTriggered(() => FinishScene());
        continueButton.RegisterCallbackButtonTriggered(() => FinishScene());
        continueButton.Focus();

        // Click through to hiddenContinueButton
        uiDocument.rootVisualElement.Query<VisualElement>()
            .ToList()
            .ForEach(visualElement => visualElement.pickingMode = visualElement is Button
                ? PickingMode.Position
                : PickingMode.Ignore);

        ActivateLayout();
        FillLayout();
    }

    private void FillLayout()
    {
        SongMeta songMeta = sceneData.SongMeta;
        string titleText = songMeta.Title.IsNullOrEmpty() ? "" : songMeta.Title;
        string artistText = songMeta.Artist.IsNullOrEmpty() ? "" : " - " + songMeta.Artist;
        songLabel.text = titleText + artistText;

        VisualElement selectedLayout = GetSelectedLayout();
        if (selectedLayout == nPlayerLayout)
        {
            PrepareNPlayerLayout();
        }

        List<VisualElement> playerUis = selectedLayout
            .Query<VisualElement>(R.UxmlNames.singingResultsPlayerUi)
            .ToList();

        singingResultsPlayerUiControls = new List<SingingResultsPlayerControl>();
        int i = 0;
        foreach (PlayerProfile playerProfile in sceneData.PlayerProfiles)
        {
            sceneData.PlayerProfileToMicProfileMap.TryGetValue(playerProfile, out MicProfile micProfile);
            PlayerScoreControllerData playerScoreData = sceneData.GetPlayerScores(playerProfile);
            SongRating songRating = GetSongRating(playerScoreData.TotalScore);

            Injector childInjector = UniInjectUtils.CreateInjector(injector);
            childInjector.AddBindingForInstance(childInjector);
            childInjector.AddBindingForInstance(playerProfile);
            childInjector.AddBindingForInstance(micProfile);
            childInjector.AddBindingForInstance(playerScoreData);
            childInjector.AddBindingForInstance(songRating);
            childInjector.AddBinding(new Binding("playerProfileIndex", new ExistingInstanceProvider<int>(i)));

            if (i < playerUis.Count)
            {
                VisualElement playerUi = playerUis[i];
                SingingResultsPlayerControl singingResultsPlayerControl = new SingingResultsPlayerControl();
                childInjector.AddBindingForInstance(Injector.RootVisualElementInjectionKey, playerUi, RebindingBehavior.Ignore);
                childInjector.Inject(singingResultsPlayerControl);
                singingResultsPlayerUiControls.Add(singingResultsPlayerControl);
            }
            i++;
        }
    }

    private void PrepareNPlayerLayout()
    {
        int playerCount = sceneData.PlayerProfiles.Count;
        // Add elements to "square similar" grid
        int columns = (int)Math.Sqrt(sceneData.PlayerProfiles.Count);
        int rows = (int)Math.Ceiling((float)playerCount / columns);
        if (sceneData.PlayerProfiles.Count == 3)
        {
            columns = 3;
            rows = 1;
        }

        int playerIndex = 0;
        for (int column = 0; column < columns; column++)
        {
            VisualElement columnElement = new VisualElement();
            columnElement.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
            columnElement.style.height = new StyleLength(Length.Percent(100f));
            columnElement.style.width = new StyleLength(Length.Percent(100f / columns));
            nPlayerLayout.Add(columnElement);

            for (int row = 0; row < rows; row++)
            {
                TemplateContainer templateContainer = nPlayerUi.CloneTree();
                VisualElement playerUi = templateContainer.Children().FirstOrDefault();
                playerUi.name = R.UxmlNames.singingResultsPlayerUi;
                playerUi.style.marginBottom = new StyleLength(20);
                playerUi.AddToClassList("singingResultUiSmall");
                if (rows > 2)
                {
                    playerUi.AddToClassList("singingResultUiSmaller");
                }
                if (rows > 3)
                {
                    playerUi.AddToClassList("singingResultUiSmallest");
                }
                columnElement.Add(playerUi);

                playerIndex++;
                if (playerIndex >= sceneData.PlayerProfiles.Count)
                {
                    // Enough, i.e., one for every player.
                    return;
                }
            }
        }
    }

    private void ActivateLayout()
    {
        List<VisualElement> layouts = new List<VisualElement>();
        layouts.Add(onePlayerLayout);
        layouts.Add(twoPlayerLayout);
        layouts.Add(nPlayerLayout);

        VisualElement selectedLayout = GetSelectedLayout();
        foreach (VisualElement layout in layouts)
        {
            layout.SetVisibleByDisplay(layout == selectedLayout);
        }
    }

    private VisualElement GetSelectedLayout()
    {
        int playerCount = sceneData.PlayerProfiles.Count;
        if (playerCount == 1)
        {
            return onePlayerLayout;
        }
        if (playerCount == 2)
        {
            return twoPlayerLayout;
        }
        return nPlayerLayout;
    }

    public void FinishScene()
    {
        if (statistics.HasHighscore(sceneData.SongMeta))
        {
            // Go to highscore scene
            HighscoreSceneData highscoreSceneData = new HighscoreSceneData();
            highscoreSceneData.SongMeta = sceneData.SongMeta;
            highscoreSceneData.Difficulty = sceneData.PlayerProfiles.FirstOrDefault().Difficulty;
            SceneNavigator.Instance.LoadScene(EScene.HighscoreScene, highscoreSceneData);
        }
        else
        {
            // No highscores to show, thus go to song select scene
            SongSelectSceneData songSelectSceneData = new SongSelectSceneData();
            songSelectSceneData.SongMeta = sceneData.SongMeta;
            SceneNavigator.Instance.LoadScene(EScene.SongSelectScene, songSelectSceneData);
        }
    }

    public List<IBinding> GetBindings()
    {
        sceneData = SceneNavigator.Instance.GetSceneDataOrThrow<SingingResultsSceneData>();

        BindingBuilder bb = new BindingBuilder();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(sceneData);
        return bb.GetBindings();
    }

    private SongRating GetSongRating(double totalScore)
    {
        foreach (SongRating songRating in SongRating.Values)
        {
            if (totalScore > songRating.ScoreThreshold)
            {
                return songRating;
            }
        }
        return SongRating.ToneDeaf;
    }

    public void UpdateTranslation()
    {
        continueButton.text = TranslationManager.GetTranslation(R.Messages.continue_);
        sceneTitle.text = TranslationManager.GetTranslation(R.Messages.singingResultsScene_title);
        singingResultsPlayerUiControls.ForEach(singingResultsPlayerUiControl => singingResultsPlayerUiControl.UpdateTranslation());
    }
}
