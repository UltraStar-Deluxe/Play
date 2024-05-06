using System.Collections.Generic;
using System.Linq;
using UniInject;
using UnityEngine;
using UnityEngine.UIElements;

public class TeamResultsUiControl : INeedInjection, IInjectionFinishedListener
{
    [Inject(Key = nameof(teamResultUi))]
    private VisualTreeAsset teamResultUi;

    [Inject]
    private SingingResultsSceneControl singingResultsSceneControl;

    [Inject(UxmlName = R.UxmlNames.teamResultsUi)]
    private VisualElement teamResultsUi;

    [Inject(UxmlName = R.UxmlNames.firstTeamUi)]
    private VisualElement firstTeamUi;

    [Inject(UxmlName = R.UxmlNames.secondTeamUi)]
    private VisualElement secondTeamUi;

    [Inject(UxmlName = R.UxmlNames.thirdTeamUi)]
    private VisualElement thirdTeamUi;

    [Inject(UxmlName = R.UxmlNames.otherTeamsScrollView)]
    private ScrollView otherTeamsScrollView;

    [Inject(UxmlName = R.UxmlNames.firstPlaceTrophyImage)]
    private VisualElement firstPlaceTrophyImage;

    private bool isVfxInitialized;

    public void OnInjectionFinished()
    {
        HideByDisplay();
        if (!singingResultsSceneControl.HasPartyModeSceneData)
        {
            return;
        }

        if (singingResultsSceneControl.HasFinalTeamResults)
        {
            FillFinalTeamResults();
        }
        else
        {
            FillIntermediateTeamResults();
        }
    }

    private void FillIntermediateTeamResults()
    {
        firstTeamUi.HideByDisplay();
        secondTeamUi.HideByDisplay();
        thirdTeamUi.HideByDisplay();

        List<PartyModeTeamSettings> otherTeams = PartyModeUtils.GetAllTeams(singingResultsSceneControl.PartyModeSceneData);
        otherTeams.Sort((a,b) =>
        {
            int aScore = PartyModeUtils.GetTeamScore(singingResultsSceneControl.PartyModeSceneData, a);
            int bScore = PartyModeUtils.GetTeamScore(singingResultsSceneControl.PartyModeSceneData, b);
            return -aScore.CompareTo(bScore);
        });

        otherTeamsScrollView.AddToClassList("intermediateTeamResults");
        otherTeamsScrollView.Clear();
        otherTeams.ForEach(team =>
        {
            VisualElement teamUi = teamResultUi.CloneTreeAndGetFirstChild();
            otherTeamsScrollView.Add(teamUi);
            FillTeamResultUi(-1, teamUi, new List<PartyModeTeamSettings> { team });
        });
    }

    private void FillFinalTeamResults()
    {
        // Find top three and remaining teams
        List<PartyModeTeamSettings> unusedTeams = PartyModeUtils.GetAllTeams(singingResultsSceneControl.PartyModeSceneData);

        List<PartyModeTeamSettings> firstTeams = PartyModeUtils.GetLeadingTeams(singingResultsSceneControl.PartyModeSceneData, unusedTeams);
        firstTeams.ForEach(usedTeam => unusedTeams.Remove(usedTeam));

        List<PartyModeTeamSettings> secondTeams = PartyModeUtils.GetLeadingTeams(singingResultsSceneControl.PartyModeSceneData, unusedTeams);
        secondTeams.ForEach(usedTeam => unusedTeams.Remove(usedTeam));

        List<PartyModeTeamSettings> thirdTeams = PartyModeUtils.GetLeadingTeams(singingResultsSceneControl.PartyModeSceneData, unusedTeams);
        thirdTeams.ForEach(usedTeam => unusedTeams.Remove(usedTeam));

        List<PartyModeTeamSettings> otherTeams = unusedTeams
            .OrderBy(otherTeam => PartyModeUtils.GetTeamScore(singingResultsSceneControl.PartyModeSceneData, otherTeam))
            .ToList();

        // Fill UI
        FillTeamResultUi(1, firstTeamUi, firstTeams);
        FillTeamResultUi(2, secondTeamUi, secondTeams);
        FillTeamResultUi(3, thirdTeamUi, thirdTeams);

        // Add other teams to scroll view
        otherTeamsScrollView.Clear();
        otherTeamsScrollView.SetVisibleByDisplay(!otherTeams.IsNullOrEmpty());

        otherTeams.ForEach(team =>
        {
            VisualElement teamUi = teamResultUi.CloneTreeAndGetFirstChild();
            teamUi.AddToClassList("mb-2");
            otherTeamsScrollView.Add(teamUi);
            FillTeamResultUi(-1, teamUi, new List<PartyModeTeamSettings> { team });
        });

        firstTeamUi.RegisterHasGeometryCallbackOneShot(_ => InitVfx());
    }

    private void FillTeamResultUi(int place, VisualElement teamUi, List<PartyModeTeamSettings> teams)
    {
        if (teams.IsNullOrEmpty())
        {
            teamUi.HideByDisplay();
            return;
        }

        teamUi.ShowByDisplay();
        Label teamNameLabel = teamUi.Q<Label>(R.UxmlNames.teamNameLabel);
        Label teamScoreLabel = teamUi.Q<Label>(R.UxmlNames.teamScoreLabel);
        VisualElement labelContainer = teamUi.Q<VisualElement>(R.UxmlNames.labelContainer);

        VisualElement knockOutOverlay = teamUi.Q<VisualElement>(R.UxmlNames.knockOutLabelOverlay);
        bool knockOutOverlayVisible = singingResultsSceneControl.PartyModeSettings.TeamSettings.IsKnockOutTournament
                                      && !PartyModeUtils.IsFinalRound(singingResultsSceneControl.PartyModeSceneData)
                                      && teams.AllMatch(team => PartyModeUtils.IsKnockedOut(singingResultsSceneControl.PartyModeSceneData, team));
        knockOutOverlay.SetVisibleByDisplay(knockOutOverlayVisible);

        teamNameLabel.SetTranslatedText(Translation.Of(teams.Select(team => team.name).JoinWith(" & ")));

        int score = PartyModeUtils.GetTeamScore(singingResultsSceneControl.PartyModeSceneData, teams.FirstOrDefault());
        teamScoreLabel.SetTranslatedText(Translation.Of(score.ToString()));

        // labelContainer.style.backgroundColor = GetPlaceColor(place);
        labelContainer.style.unityBackgroundImageTintColor = GetPlaceColor(place);
    }

    private Color GetPlaceColor(int place)
    {
        return place switch
        {
            1 => Colors.CreateColor("#B0B134"),
            2 => Colors.silver,
            3 => Colors.bronze,
            _ => Colors.grey
        };
    }

    private void InitVfx()
    {
        if (isVfxInitialized)
        {
            return;
        }
        isVfxInitialized = true;

        // Create particle effect for first place
        VfxManager.CreateParticleEffect(new ParticleEffectConfig()
        {
            particleEffect = EParticleEffect.LightGlowALoop,
            panelPos = firstPlaceTrophyImage.worldBound.center,
            scale = 0.4f,
            loop = true,
            isBackground = true,
            target = firstPlaceTrophyImage,
            hideAndShowWithTarget = true,
        });
    }

    public void HideByDisplay()
    {
        teamResultsUi.HideByDisplay();
    }

    public bool IsVisibleByDisplay()
    {
        return teamResultsUi.IsVisibleByDisplay();
    }
}
