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

    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongPreviewControl songPreviewControl;

    [InjectedInInspector]
    public VisualTreeAsset highscoreEntryUi;

    [InjectedInInspector]
    public AudioSource crowdCheerAudioSource;
    
    [Inject(UxmlName = R.UxmlNames.artistLabel)]
    private Label artistLabel;

    [Inject(UxmlName = R.UxmlNames.titleLabel)]
    private Label titleLabel;
    
    [Inject(UxmlName = R.UxmlNames.coverImage)]
    private VisualElement coverImage;
    
    [Inject(UxmlName = R.UxmlNames.onePlayerLayout)]
    private VisualElement onePlayerLayout;

    [Inject(UxmlName = R.UxmlNames.twoPlayerLayout)]
    private VisualElement twoPlayerLayout;

    [Inject(UxmlName = R.UxmlNames.nPlayerLayout)]
    private VisualElement nPlayerLayout;

    [Inject(UxmlName = R.UxmlNames.continueButton)]
    private Button continueButton;

    [Inject(UxmlName = R.UxmlNames.restartButton)]
    private Button restartButton;
    
    [Inject(UxmlName = R.UxmlNames.background)]
    private VisualElement background;
    
    [Inject(UxmlName = R.UxmlNames.showCurrentResultsButton)]
    private ToggleButton showCurrentResultsButton;
    
    [Inject(UxmlName = R.UxmlNames.showHighscoreButton)]
    private ToggleButton showHighscoreButton;
    
    [Inject(UxmlName = R.UxmlNames.playerResultsRoot)]
    private VisualElement playerResultsRoot;
    
    [Inject(UxmlName = R.UxmlNames.highscoresRoot)]
    private VisualElement highscoresRoot;
    
    [Inject]
    private Statistics statistics;

    [Inject]
    private UIDocument uiDocument;

    [Inject]
    private Injector injector;

    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private SongQueueManager songQueueManager;

	[Inject]
    private SingingResultsSceneData sceneData;
    
    private readonly List<SingingResultsPlayerControl> singingResultsPlayerUiControls = new();
    private readonly List<GameObject> particleSystems = new();
    private readonly NextGameRoundUiControl nextGameRoundUiControl = new();
    private readonly TeamResultsUiControl teamResultsUiControl = new();

    private bool ShowHighScoresNext => !sceneData.IsMedley
                                       && !HasPartyModeSceneData
                                       && statistics.HasHighscore(sceneData.SongMetas.LastOrDefault());

    private readonly SingingResultsHighscoreControl highscoreControl = new();
    
    public static SingingResultsSceneControl Instance
    {
        get
        {
            return FindObjectOfType<SingingResultsSceneControl>();
        }
    }

    public PartyModeSettings PartyModeSettings => sceneData.partyModeSceneData.PartyModeSettings;
    public bool HasPartyModeSceneData => PartyModeSceneData != null;
    public PartyModeSceneData PartyModeSceneData => sceneData.partyModeSceneData;
    public bool HasFinalTeamResults => PartyModeUtils.IsFinalRound(PartyModeSceneData);

    public void OnInjectionFinished()
    {
        GivePartyModeTeamPoints();
        KnockOutPartyTeams();

        injector.Inject(nextGameRoundUiControl);
        injector.Inject(teamResultsUiControl);
        injector.Inject(highscoreControl);

        if (ShowHighScoresNext)
        {
            nextGameRoundUiControl.HideNextGameRoundUi();
        }
    }

    private void Start()
    {      
        TabGroupControl tabGroupControl = new();
        tabGroupControl.AddTabGroupButton(showCurrentResultsButton, playerResultsRoot);
        tabGroupControl.AddTabGroupButton(showHighscoreButton, highscoresRoot);
        tabGroupControl.ShowContainer(playerResultsRoot);
        showCurrentResultsButton.RegisterCallbackButtonTriggered(_ =>
        {
            particleSystems.ForEach(it => it.SetActive(true));
        });
        showHighscoreButton.RegisterCallbackButtonTriggered(_ =>
        {
            particleSystems.ForEach(it => it.SetActive(false));
            highscoreControl.Init();
        });
        
        restartButton.RegisterCallbackButtonTriggered(_ => RestartSingScene());
        
        background.RegisterCallback<PointerUpEvent>(evt => Continue());
        continueButton.RegisterCallbackButtonTriggered(_ => Continue());
        continueButton.Focus();

        InitClickThoughToHiddenContinueButton();

        songAudioPlayer.Init(sceneData.SongMetas.LastOrDefault());

        songPreviewControl.PreviewDelayInSeconds = 0;
        songPreviewControl.AudioFadeInDurationInSeconds = 2;
        songPreviewControl.VideoFadeInDurationInSeconds = 2;
        songPreviewControl.StartSongPreview(sceneData.SongMetas.LastOrDefault());

        ActivateLayout();
        FillLayout();

        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(1, () => InitVfx()));
    }

    private void InitVfx()
    {
        List<PlayerProfile> unusedPlayerProfiles = sceneData.PlayerProfiles.ToList();
        List<PlayerProfile> firstPlayers = GetTopPlayers(unusedPlayerProfiles);
        singingResultsPlayerUiControls
            .Where(it => firstPlayers.Contains(it.PlayerProfile)
                && sceneData.GetPlayerScores(it.PlayerProfile).TotalScore > 0)
            .ForEach(it =>
            {
                GameObject newParticleSystem = VfxManager.CreateParticleSystem(
                    EParticleEffect.LightGlowALoop,
                    it.PlayerImage.worldBound.center,
                    0.4f,
                    true,
                    true);
                particleSystems.Add(newParticleSystem);
            });
    }

    private void RestartSingScene()
    {
        SingSceneData singSceneData = SceneNavigator.GetSceneData(new SingSceneData());
        singSceneData.SongMetas = sceneData.SongMetas;
        sceneNavigator.LoadScene(EScene.SingScene, singSceneData);
    }
    
    private void KnockOutPartyTeams()
    {
        if (!HasPartyModeSceneData
            || !PartyModeSettings.teamSettings.isKnockOutTournament)
        {
            return;
        }

        PartyModeTeamSettings knockedOutTeam = GetKnockedOutTeam(sceneData.PlayerProfiles.ToList());
        if (knockedOutTeam != null)
        {
            PartyModeSceneData.teamToIsKnockedOutMap[knockedOutTeam] = true;
        }
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
        SongMeta songMeta = sceneData.SongMetas.LastOrDefault();
        artistLabel.text = songMeta.Artist;
        titleLabel.text = songMeta.Title;
        SongMetaImageUtils.SetCoverOrBackgroundImage(songMeta, coverImage);
        
        VisualElement selectedLayout = GetSelectedLayout();
        if (selectedLayout == nPlayerLayout)
        {
            PrepareNPlayerLayout();
        }

        List<VisualElement> playerUis = selectedLayout
            .Query<VisualElement>(R.UxmlNames.singingResultsPlayerUiRoot)
            .ToList();

        singingResultsPlayerUiControls.Clear();
        int i = 0;
        foreach (PlayerProfile playerProfile in sceneData.PlayerProfiles)
        {
            sceneData.PlayerProfileToMicProfileMap.TryGetValue(playerProfile, out MicProfile micProfile);
            PlayerScoreControlData playerScoreData = sceneData.GetPlayerScores(playerProfile);
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
        int playerCount = sceneData.PlayerProfiles.Count;
        
        // Add elements to "square similar" grid
        int columns = (int)Math.Sqrt(sceneData.PlayerProfiles.Count);
        int rows = (int)Math.Ceiling((float)playerCount / columns);
        if (playerCount == 3)
        {
            columns = 3;
            rows = 1;
        }

        int playerIndex = 0;
        for (int column = 0; column < columns; column++)
        {
            VisualElement columnElement = new();
            columnElement.name = "column";
            columnElement.AddToClassList("singingResultsPlayerUiColumn");
            columnElement.style.flexDirection = new StyleEnum<FlexDirection>(FlexDirection.Column);
            columnElement.style.height = new StyleLength(Length.Percent(100f));
            columnElement.style.width = new StyleLength(Length.Percent(100f / columns));
            nPlayerLayout.Add(columnElement);

            for (int row = 0; row < rows; row++)
            {
                VisualElement playerUi = nPlayerUi.CloneTree().Children().FirstOrDefault();
                playerUi.style.height = new StyleLength(Length.Percent(100f / rows));
                
                for (int i = 1; i <= playerCount; i++)
                {
                    playerUi.AddToClassList($"singingResultUi-{i}");
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
        List<VisualElement> layouts = new();
        layouts.Add(onePlayerLayout);
        layouts.Add(twoPlayerLayout);
        layouts.Add(nPlayerLayout);

        VisualElement selectedLayout = GetSelectedLayout();
        foreach (VisualElement layout in layouts)
        {
            layout.SetVisibleByDisplay(layout == selectedLayout);
            if (layout != selectedLayout
                || layout == nPlayerLayout)
            {
                layout.Clear();
            }
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

    private void FinishScene()
    {
        if (ShowHighScoresNext)
        {
            // Go to highscore scene
            HighscoreSceneData highscoreSceneData = new();
            highscoreSceneData.SongMeta = sceneData.SongMetas.LastOrDefault();
            highscoreSceneData.Difficulty = sceneData.PlayerProfiles.FirstOrDefault().Difficulty;
            highscoreSceneData.partyModeSceneData = PartyModeSceneData;
            sceneNavigator.LoadScene(EScene.HighscoreScene, highscoreSceneData);
        }
        else if (HasPartyModeSceneData && HasFinalTeamResults)
        {
            // Go to party mode config
            sceneNavigator.LoadScene(EScene.PartyModeScene);
        }
        else if (!songQueueManager.IsSongQueueEmpty)
        {
            // Start next game round
            if (HasPartyModeSceneData)
            {
                // Increase party round index
                PartyModeSceneData.currentRoundIndex++;
            }

            SingSceneData singSceneData = songQueueManager.CreateNextSingSceneData(sceneData.partyModeSceneData);
            sceneNavigator.LoadScene(EScene.SingScene, singSceneData);
        }
        else
        {
            // Go to song select scene
            if (HasPartyModeSceneData)
            {
                // Increase party round index
                PartyModeSceneData.currentRoundIndex++;
            }

            SongSelectSceneData songSelectSceneData = new();
            songSelectSceneData.SongMeta = sceneData.SongMetas.LastOrDefault();
            songSelectSceneData.partyModeSceneData = sceneData.partyModeSceneData;
            sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
        }
    }

    public void Continue()
    {
        if (HasPartyModeSceneData
            && !teamResultsUiControl.IsVisibleByDisplay())
        {
            // Show team result
            playerResultsRoot.HideByDisplay();
            teamResultsUiControl.ShowByDisplay();

            if (HasFinalTeamResults)
            {
                // Play audio clip
                crowdCheerAudioSource.volume = NumberUtils.PercentToFactor(settings.AudioSettings.VolumePercent);
                crowdCheerAudioSource.Play();
            }
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
        bb.BindExistingInstance(SceneNavigator.GetSceneDataOrThrow<SingingResultsSceneData>());
        bb.BindExistingInstance(songAudioPlayer);
        bb.BindExistingInstance(songPreviewControl);
        bb.Bind(nameof(highscoreEntryUi)).ToExistingInstance(highscoreEntryUi);
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
        singingResultsPlayerUiControls.ForEach(singingResultsPlayerUiControl => singingResultsPlayerUiControl.UpdateTranslation());
        
        if (HasPartyModeSceneData)
        {
            //sceneTitle.text += $" - {PartyModeSceneData.currentRoundIndex + 1} / {PartyModeSettings.roundCount}";
        }
    }

    private void OnDestroy()
    {
        singingResultsPlayerUiControls.ForEach(it => it.Dispose());
    }
    
    private void GivePartyModeTeamPoints()
    {
        if (!HasPartyModeSceneData)
        {
            return;
        }

        // Determine first best and second best players of this round
        List<PlayerProfile> unusedPlayerProfiles = sceneData.PlayerProfiles.ToList();
        List<PlayerProfile> firstPlayers = GetTopPlayers(unusedPlayerProfiles);
        firstPlayers.ForEach(playerProfile => unusedPlayerProfiles.Remove(playerProfile));

        List<PlayerProfile> secondPlayers = GetTopPlayers(unusedPlayerProfiles);
        secondPlayers.ForEach(playerProfile => unusedPlayerProfiles.Remove(playerProfile));

        // Find corresponding teams of first and second best players
        List<PartyModeTeamSettings> firstTeams = firstPlayers
            .Select(playerProfile => PartyModeUtils.GetTeam(PartyModeSceneData, playerProfile))
            .Where(team => team != null)
            .ToList();
        List<PartyModeTeamSettings> secondTeams = secondPlayers
            .Select(playerProfile => PartyModeUtils.GetTeam(PartyModeSceneData, playerProfile))
            .Where(team => team != null)
            .ToList();

        // First teams receive 2 points. Second teams receive 1 point.
        firstTeams.ForEach(team => PartyModeSceneData.teamToScoreMap[team] = PartyModeUtils.GetTeamScore(PartyModeSceneData, team) + 2);
        secondTeams.ForEach(team => PartyModeSceneData.teamToScoreMap[team] = PartyModeUtils.GetTeamScore(PartyModeSceneData, team) + 1);
    }

    private List<PlayerProfile> GetTopPlayers(List<PlayerProfile> playerProfiles)
    {
        if (playerProfiles.IsNullOrEmpty())
        {
            return new();
        }

        int highestScore = playerProfiles
            .Select(playerProfile => sceneData.GetPlayerScores(playerProfile).TotalScore)
            .Max();
        return playerProfiles
            .Where(playerProfile => sceneData.GetPlayerScores(playerProfile).TotalScore == highestScore)
            .ToList();
    }

    private PartyModeTeamSettings GetKnockedOutTeam(List<PlayerProfile> playerProfiles)
    {
        if (playerProfiles.IsNullOrEmpty())
        {
            return new();
        }

        List<PartyModeTeamSettings> teams = playerProfiles
            .Select(playerProfile => PartyModeUtils.GetTeam(PartyModeSceneData, playerProfile))
            .ToList();
        if (teams.IsNullOrEmpty())
        {
            return null;
        }

        int lowestTeamScore = teams.Select(team => PartyModeUtils.GetTeamScore(PartyModeSceneData, team))
            .Min();
        List<PartyModeTeamSettings> lowestTeams = teams
            .Where(team => PartyModeUtils.GetTeamScore(PartyModeSceneData, team) == lowestTeamScore)
            .ToList();
        if (lowestTeams.IsNullOrEmpty())
        {
            return null;
        }

        lowestTeams.Sort((a, b) => GetCurrentRoundPoints(a).CompareTo(GetCurrentRoundPoints(b)));
        PartyModeTeamSettings lowestTeam = lowestTeams.FirstOrDefault();
        return lowestTeam;
    }

    private PlayerProfile GetPlayerProfileOfThisRound(PartyModeTeamSettings team)
    {
        List<PlayerProfile> playerProfiles = sceneData.PlayerProfiles.ToList();
        PlayerProfile playerProfileOfTeam = playerProfiles.FirstOrDefault(playerProfile => PartyModeUtils.GetTeam(PartyModeSceneData, playerProfile) == team);
        return playerProfileOfTeam;
    }

    private int GetCurrentRoundPoints(PartyModeTeamSettings team)
    {
        PlayerProfile playerProfileOfTeam = GetPlayerProfileOfThisRound(team);
        if (playerProfileOfTeam == null)
        {
            return 0;
        }

        return sceneData.GetPlayerScores(playerProfileOfTeam).TotalScore;
    }
}
