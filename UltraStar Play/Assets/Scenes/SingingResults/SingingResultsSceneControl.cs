using System;
using System.Collections.Generic;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
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

    [InjectedInInspector]
    public List<SongRatingImageReference> songRatingImageReferences;

    [InjectedInInspector]
    public SongAudioPlayer songAudioPlayer;

    [InjectedInInspector]
    public SongPreviewControl songPreviewControl;

    [InjectedInInspector]
    public VisualTreeAsset highscoreEntryUi;

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
    private Button showCurrentResultsButton;
    
    [Inject(UxmlName = R.UxmlNames.showHighscoreButton)]
    private Button showHighscoreButton;
    
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
    private SingingResultsSceneData sceneData;

    private List<SingingResultsPlayerControl> singingResultsPlayerUiControls = new();

    private readonly SingingResultsHighscoreControl highscoreControl = new();
    
    public static SingingResultsSceneControl Instance
    {
        get
        {
            return FindObjectOfType<SingingResultsSceneControl>();
        }
    }

    void Start()
    {
        injector.Inject(highscoreControl);
        
        background.RegisterCallback<PointerUpEvent>(evt => FinishScene());
        continueButton.RegisterCallbackButtonTriggered(() => FinishScene());
        continueButton.Focus();

        TabGroupControl tabGroupControl = new();
        tabGroupControl.AddTabGroupButton(showCurrentResultsButton, playerResultsRoot);
        tabGroupControl.AddTabGroupButton(showHighscoreButton, highscoresRoot);
        tabGroupControl.ShowContainer(playerResultsRoot);
        showHighscoreButton.RegisterCallbackButtonTriggered(() => highscoreControl.Init());
        
        restartButton.RegisterCallbackButtonTriggered(() => RestartSingScene());

        // Click through to background
        background.Query<VisualElement>().ForEach(visualElement =>
        {
            if (visualElement is not Button
                && visualElement != background)
            {
                visualElement.pickingMode = PickingMode.Ignore;
            }
        });

        songAudioPlayer.Init(sceneData.SongMeta);

        songPreviewControl.PreviewDelayInSeconds = 0;
        songPreviewControl.AudioFadeInDurationInSeconds = 2;
        songPreviewControl.VideoFadeInDurationInSeconds = 2;
        songPreviewControl.StartSongPreview(sceneData.SongMeta);
        
        ActivateLayout();
        FillLayout();
    }

    private void RestartSingScene()
    {
        SingSceneData singSceneData = SceneNavigator.GetSceneData(new SingSceneData());
        singSceneData.SelectedSongMeta = sceneData.SongMeta;
        sceneNavigator.LoadScene(EScene.SingScene, singSceneData);
    }

    private void FillLayout()
    {
        SongMeta songMeta = sceneData.SongMeta;
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

        singingResultsPlayerUiControls = new List<SingingResultsPlayerControl>();
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

    public void FinishScene()
    {
        if (statistics.HasHighscore(sceneData.SongMeta))
        {
            // Go to highscore scene
            HighscoreSceneData highscoreSceneData = new();
            highscoreSceneData.SongMeta = sceneData.SongMeta;
            highscoreSceneData.Difficulty = sceneData.PlayerProfiles.FirstOrDefault().Difficulty;
            sceneNavigator.LoadScene(EScene.HighscoreScene, highscoreSceneData);
        }
        else
        {
            // No highscores to show, thus go to song select scene
            SongSelectSceneData songSelectSceneData = new();
            songSelectSceneData.SongMeta = sceneData.SongMeta;
            sceneNavigator.LoadScene(EScene.SongSelectScene, songSelectSceneData);
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
    }

    private void OnDestroy()
    {
        singingResultsPlayerUiControls.ForEach(it => it.Dispose());
    }
}
