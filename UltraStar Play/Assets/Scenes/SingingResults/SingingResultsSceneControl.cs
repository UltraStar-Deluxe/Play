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

public class SingingResultsSceneControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener, IBinder, ITranslator
{
    [InjectedInInspector]
    public VisualTreeAsset nPlayerUi;

    [InjectedInInspector]
    public VisualTreeAsset teamResultUi;

    [InjectedInInspector]
    public List<SongRatingImageReference> songRatingImageReferences;

    [Inject(UxmlName = R.UxmlNames.sceneTitle)]
    private Label sceneTitle;

    [Inject(UxmlName = R.UxmlNames.sceneSubtitle)]
    private Label songLabel;

    [Inject(UxmlName = R.UxmlNames.playerResultsContainer)]
    public VisualElement playerResultsContainer;

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

    [Inject]
    private Settings settings;

    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private GameRoundManager gameRoundManager;

    private SingingResultsSceneData sceneData;
    public SingingResultsSceneData SceneData
    {
        get
        {
            if (sceneData == null)
            {
                sceneData = SceneNavigator.Instance.GetSceneDataOrThrow<SingingResultsSceneData>();
            }

            return sceneData;
        }
    }

    private readonly List<SingingResultsPlayerControl> singingResultsPlayerUiControls = new();
    private readonly NextGameRoundUiControl nextGameRoundUiControl = new();
    private readonly TeamResultsUiControl teamResultsUiControl = new();

    private bool ShowHighScoresNext => !SceneData.IsMedley
                                       && !HasPartyModeSettings
                                       && statistics.HasHighscore(SceneData.SongMetas.LastOrDefault());

    public static SingingResultsSceneControl Instance
    {
        get
        {
            return FindObjectOfType<SingingResultsSceneControl>();
        }
    }

    public PartyModeSettings PartyModeSettings => SceneData.partyModeSettings;
    public bool HasPartyModeSettings => PartyModeSettings != null;
    public bool HasFinalTeamResults => PartyModeUtils.IsFinalRound(PartyModeSettings);

    public void OnInjectionFinished()
    {
        GivePartyModeTeamPoints();

        injector.Inject(nextGameRoundUiControl);
        injector.Inject(teamResultsUiControl);

        if (ShowHighScoresNext)
        {
            nextGameRoundUiControl.HideNextGameRoundUi();
        }
    }

    private void Start()
    {
        hiddenContinueButton.RegisterCallbackButtonTriggered(() => Continue());
        continueButton.RegisterCallbackButtonTriggered(() => Continue());
        continueButton.Focus();

        InitClickThoughToHiddenContinueButton();

        ActivateLayout();
        FillLayout();
    }

    private void InitClickThoughToHiddenContinueButton()
    {
        uiDocument.rootVisualElement.Query<VisualElement>()
            .ForEach(visualElement =>
            {
                visualElement.pickingMode = visualElement is Button
                    ? PickingMode.Position
                    : PickingMode.Ignore;
            });

        // Reset scroll views. Otherwise they do not work.
        uiDocument.rootVisualElement.Query<ScrollView>()
            .ForEach(scrollView =>
            {
                scrollView.pickingMode = PickingMode.Position;
                scrollView.Query<VisualElement>()
                    .ForEach(scrollViewChild => scrollViewChild.pickingMode = PickingMode.Position);
            });
    }

    private void FillLayout()
    {
        if (SceneData.IsMedley)
        {
            songLabel.text = TranslationManager.GetTranslation(R.Messages.score_total);
        }
        else
        {
            SongMeta songMeta = SceneData.SongMetas.LastOrDefault();
            string titleText = songMeta.Title.IsNullOrEmpty() ? "" : songMeta.Title;
            string artistText = songMeta.Artist.IsNullOrEmpty() ? "" : " - " + songMeta.Artist;
            songLabel.text = titleText + artistText;
        }

        VisualElement selectedLayout = GetSelectedLayout();
        if (selectedLayout == nPlayerLayout)
        {
            PrepareNPlayerLayout();
        }

        List<VisualElement> playerUis = selectedLayout
            .Query<VisualElement>(R.UxmlNames.singingResultsPlayerUi)
            .ToList();

        singingResultsPlayerUiControls.Clear();
        int i = 0;
        foreach (PlayerProfile playerProfile in SceneData.PlayerProfiles)
        {
            SceneData.PlayerProfileToMicProfileMap.TryGetValue(playerProfile, out MicProfile micProfile);
            PlayerScoreControlData playerScoreData = SceneData.GetPlayerScores(playerProfile);
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
                SingingResultsPlayerControl singingResultsPlayerControl = new();
                childInjector.AddBindingForInstance(Injector.RootVisualElementInjectionKey, playerUi, RebindingBehavior.Ignore);
                childInjector.Inject(singingResultsPlayerControl);
                singingResultsPlayerUiControls.Add(singingResultsPlayerControl);
            }
            i++;
        }
    }

    private void PrepareNPlayerLayout()
    {
        int playerCount = SceneData.PlayerProfiles.Count;
        // Add elements to "square similar" grid
        int columns = (int)Math.Sqrt(SceneData.PlayerProfiles.Count);
        int rows = (int)Math.Ceiling((float)playerCount / columns);
        if (SceneData.PlayerProfiles.Count == 3)
        {
            columns = 3;
            rows = 1;
        }

        int playerIndex = 0;
        for (int column = 0; column < columns; column++)
        {
            VisualElement columnElement = new();
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
                if (playerIndex >= SceneData.PlayerProfiles.Count)
                {
                    // Enough, i.e., one for every player.
                    return;
                }
            }
        }
    }

    private void ActivateLayout()
    {
        List<VisualElement> layouts = new();
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
        int playerCount = SceneData.PlayerProfiles.Count;
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

    private void FinishScene()
    {
        if (ShowHighScoresNext)
        {
            // Go to highscore scene
            HighscoreSceneData highscoreSceneData = new();
            highscoreSceneData.SongMeta = SceneData.SongMetas.LastOrDefault();
            highscoreSceneData.Difficulty = SceneData.PlayerProfiles.FirstOrDefault().Difficulty;
            sceneNavigator.LoadScene(EScene.HighscoreScene, highscoreSceneData);
        }
        else if (!HasPartyModeSettings && gameRoundManager.HasGameRounds)
        {
            // Start next game round
            gameRoundManager.StartNextGameRound();
        }
        else if (HasPartyModeSettings && HasFinalTeamResults)
        {
            // Go to party mode config
            sceneNavigator.LoadScene(EScene.PartyModeScene);
        }
        else
        {
            // Go to song select scene
            if (HasPartyModeSettings)
            {
                // Increase party round index
                PartyModeSettings.currentRoundIndex++;
            }

            SongSelectSceneData songSelectSceneData = new();
            songSelectSceneData.SongMeta = SceneData.SongMetas.LastOrDefault();
            songSelectSceneData.PartyModeSettings = SceneData.partyModeSettings;
            sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
        }
    }

    public void Continue()
    {
        if (HasPartyModeSettings
            && HasFinalTeamResults
            && !teamResultsUiControl.IsVisibleByDisplay())
        {
            // Show team result
            playerResultsContainer.HideByDisplay();
            teamResultsUiControl.ShowByDisplay();
        }
        else
        {
            FinishScene();
        }
    }

    public List<IBinding> GetBindings()
    {
        BindingBuilder bb = new();
        bb.BindExistingInstance(this);
        bb.BindExistingInstance(gameObject);
        bb.BindExistingInstance(SceneData);
        bb.BindExistingInstance(nextGameRoundUiControl);
        bb.BindExistingInstance(teamResultsUiControl);
        bb.Bind(nameof(teamResultUi)).ToExistingInstance(teamResultUi);
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

    private void GivePartyModeTeamPoints()
    {
        if (!HasPartyModeSettings)
        {
            return;
        }

        // Determine first best and second best players of this round
        List<PlayerProfile> unusedPlayerProfiles = SceneData.PlayerProfiles.ToList();
        List<PlayerProfile> firstPlayers = GetLeadingPlayers(unusedPlayerProfiles);
        firstPlayers.ForEach(playerProfile => unusedPlayerProfiles.Remove(playerProfile));

        List<PlayerProfile> secondPlayers = GetLeadingPlayers(unusedPlayerProfiles);
        secondPlayers.ForEach(playerProfile => unusedPlayerProfiles.Remove(playerProfile));

        // Find corresponding teams of first and second best players
        List<PartyModeTeamSettings> firstTeams = firstPlayers
            .Select(playerProfile => PartyModeUtils.GetTeam(PartyModeSettings, playerProfile))
            .ToList();
        List<PartyModeTeamSettings> secondTeams = secondPlayers
            .Select(playerProfile => PartyModeUtils.GetTeam(PartyModeSettings, playerProfile))
            .ToList();

        // First teams receive 2 points. Second teams receive 1 point.
        firstTeams.ForEach(team => PartyModeSettings.teamToScoreMap[team] = PartyModeUtils.GetTeamScore(PartyModeSettings, team) + 2);
        secondTeams.ForEach(team => PartyModeSettings.teamToScoreMap[team] = PartyModeUtils.GetTeamScore(PartyModeSettings, team) + 1);
    }

    private List<PlayerProfile> GetLeadingPlayers(List<PlayerProfile> playerProfiles)
    {
        if (playerProfiles.IsNullOrEmpty())
        {
            return new();
        }

        int highestScore = playerProfiles
            .Select(playerProfile => SceneData.GetPlayerScores(playerProfile).TotalScore)
            .Max();
        return playerProfiles
            .Where(playerProfile => SceneData.GetPlayerScores(playerProfile).TotalScore == highestScore)
            .ToList();
    }
}
