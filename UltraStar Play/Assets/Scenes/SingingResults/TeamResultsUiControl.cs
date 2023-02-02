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

    public void OnInjectionFinished()
    {
        HideByDisplay();
        if (!singingResultsSceneControl.HasPartyModeSettings)
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
        
        List<PartyModeTeamSettings> otherTeams = PartyModeUtils.GetAllTeams(singingResultsSceneControl.PartyModeSettings);
        otherTeams.Sort((a,b) =>
        {
            int aScore = PartyModeUtils.GetTeamScore(singingResultsSceneControl.PartyModeSettings, a);
            int bScore = PartyModeUtils.GetTeamScore(singingResultsSceneControl.PartyModeSettings, b);
            return aScore.CompareTo(bScore);
        });

        otherTeamsScrollView.style.width = new StyleLength(new Length(100, LengthUnit.Percent));
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
        List<PartyModeTeamSettings> unusedTeams = PartyModeUtils.GetAllTeams(singingResultsSceneControl.PartyModeSettings);

        List<PartyModeTeamSettings> firstTeams = PartyModeUtils.GetLeadingTeams(singingResultsSceneControl.PartyModeSettings, unusedTeams);
        firstTeams.ForEach(usedTeam => unusedTeams.Remove(usedTeam));

        List<PartyModeTeamSettings> secondTeams = PartyModeUtils.GetLeadingTeams(singingResultsSceneControl.PartyModeSettings, unusedTeams);
        secondTeams.ForEach(usedTeam => unusedTeams.Remove(usedTeam));

        List<PartyModeTeamSettings> thirdTeams = PartyModeUtils.GetLeadingTeams(singingResultsSceneControl.PartyModeSettings, unusedTeams);
        thirdTeams.ForEach(usedTeam => unusedTeams.Remove(usedTeam));

        List<PartyModeTeamSettings> otherTeams = unusedTeams
            .OrderBy(otherTeam => PartyModeUtils.GetTeamScore(singingResultsSceneControl.PartyModeSettings, otherTeam))
            .ToList();

        // Fill UI
        FillTeamResultUi(1, firstTeamUi, firstTeams);
        FillTeamResultUi(2, secondTeamUi, secondTeams);
        FillTeamResultUi(3, thirdTeamUi, thirdTeams);

        // Add other teams to scroll view
        otherTeamsScrollView.Clear();
        otherTeams.ForEach(team =>
        {
            VisualElement teamUi = teamResultUi.CloneTreeAndGetFirstChild();
            otherTeamsScrollView.Add(teamUi);
            FillTeamResultUi(-1, teamUi, new List<PartyModeTeamSettings> { team });
        });
    }

    private void FillTeamResultUi(int place, VisualElement teamUi, List<PartyModeTeamSettings> teams)
    {
        if (teams.IsNullOrEmpty())
        {
            teamUi.HideByDisplay();
            return;
        }

        teamUi.ShowByDisplay();
        VisualElement trophyIcon = teamUi.Q<VisualElement>(R.UxmlNames.trophyIcon);
        Label teamNameLabel = teamUi.Q<Label>(R.UxmlNames.teamNameLabel);
        Label teamScoreLabel = teamUi.Q<Label>(R.UxmlNames.teamScoreLabel);
        VisualElement labelContainer = teamUi.Q<VisualElement>(R.UxmlNames.labelContainer);

        teamNameLabel.text = teams.Select(team => team.name).JoinWith(" & ");

        int score = PartyModeUtils.GetTeamScore(singingResultsSceneControl.PartyModeSettings, teams.FirstOrDefault());
        teamScoreLabel.text = score.ToString();

        labelContainer.style.backgroundColor = GetPlaceColor(place);
        if (place is 1 or 2 or 3)
        {
            trophyIcon.ShowByDisplay();
            trophyIcon.ShowByVisibility();
            trophyIcon.style.color = new StyleColor(GetPlaceColor(place));
        }
        else
        {
            trophyIcon.HideByVisibility();
        }
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

    public void ShowByDisplay()
    {
        teamResultsUi.ShowByDisplay();
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
