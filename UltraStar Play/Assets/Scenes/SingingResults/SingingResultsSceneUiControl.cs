using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using ProTrans;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;
using IBinding = UniInject.IBinding;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingingResultsSceneUiControl : MonoBehaviour, INeedInjection, IBinder, ITranslator
{
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

    [Inject]
    private Statistics statistics;

    [Inject]
    private Injector injector;

    private SingingResultsSceneData sceneData;

    public static SingingResultsSceneUiControl Instance
    {
        get
        {
            return FindObjectOfType<SingingResultsSceneUiControl>();
        }
    }

    void Start()
    {
        continueButton.RegisterCallbackButtonTriggered(() => FinishScene());
        continueButton.Focus();

        ActivateLayout();
        FillLayout();
    }

    private void FillLayout()
    {
        SongMeta songMeta = sceneData.SongMeta;
        string titleText = songMeta.Title.IsNullOrEmpty() ? "" : songMeta.Title;
        string artistText = songMeta.Artist.IsNullOrEmpty() ? "" : " - " + songMeta.Artist;
        songLabel.text = titleText + artistText;

        // int i = 0;
        // foreach (PlayerProfile playerProfile in sceneData.PlayerProfiles)
        // {
        //     sceneData.PlayerProfileToMicProfileMap.TryGetValue(playerProfile, out MicProfile micProfile);
        //     PlayerScoreControllerData playerScoreData = sceneData.GetPlayerScores(playerProfile);
        //     SongRating songRating = GetSongRating(playerScoreData.TotalScore);
        //
        //     Injector childInjector = UniInjectUtils.CreateInjector(injector);
        //     childInjector.AddBindingForInstance(playerProfile);
        //     childInjector.AddBindingForInstance(micProfile);
        //     childInjector.AddBindingForInstance(playerScoreData);
        //     childInjector.AddBindingForInstance(songRating);
        //     childInjector.AddBinding(new Binding("playerProfileIndex", new ExistingInstanceProvider<int>(i)));
        //
        //     if (i < uiControllers.Length)
        //     {
        //         childInjector.InjectAllComponentsInChildren(uiControllers[i], true);
        //     }
        //     i++;
        // }
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
            layout.SetVisible(layout == selectedLayout);
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
    }
}
