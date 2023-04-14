using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DefaultSingingResultsSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    [Range(1, 16)]
    public int playerCount = 1;

    [Range(0, 8)]
    public int partyModeTeams = 5;

    public Vector2 scoreRange = new Vector2(2000, 8000);
    
    public bool isLastPartyModeRound;
    public bool isKnockOutTournament;
    
    public SceneData GetDefaultSceneData()
    {
        SingingResultsSceneData data = new();

        SongMetaManager.Instance.WaitUntilSongScanFinished();

        Settings settings = SettingsManager.Instance.Settings;

        data.SongMetas = new List<SongMeta> { SongMetaManager.Instance.GetFirstSongMeta() };
        data.SongDurationInMillis = 120 * 1000;

        List<PlayerProfile> settingsPlayerProfiles = settings.PlayerProfiles;
        for (int i = 0; i < playerCount && i < settingsPlayerProfiles.Count; i++)
        {
            PlayerProfile playerProfile = settingsPlayerProfiles[i];
            data.AddPlayerScores(playerProfile, CreatePlayerScoreData());
            
            if (settings.MicProfiles.Count > i)
            {
                data.PlayerProfileToMicProfileMap[playerProfile] = settings.MicProfiles[i];
            }
        }

        if (partyModeTeams > 0)
        {
            data.partyModeSceneData = CreatePartyModeSceneData();
        }
        return data;
    }

    private PlayerScoreControlData CreatePlayerScoreData()
    {
        PlayerScoreControlData playerScoreData = new();
        playerScoreData.NormalNotesTotalScore = (int)Random.Range(scoreRange.x / 3, scoreRange.y / 3);
        playerScoreData.GoldenNotesTotalScore = (int)Random.Range(scoreRange.x / 3, scoreRange.y / 3);
        playerScoreData.PerfectSentenceBonusTotalScore = (int)Random.Range(scoreRange.x / 3, scoreRange.y / 3);
        playerScoreData.TotalScore = playerScoreData.NormalNotesTotalScore
                                     + playerScoreData.GoldenNotesTotalScore
                                     + playerScoreData.PerfectSentenceBonusTotalScore;

        playerScoreData.NormalNoteLengthTotal = 80;
        playerScoreData.GoldenNoteLengthTotal = 20;
        playerScoreData.PerfectSentenceCount = 10;

        playerScoreData.NormalBeatData.GoodBeats = 30;
        playerScoreData.NormalBeatData.PerfectBeats = 10;
        playerScoreData.GoldenBeatData.GoodBeats = 5;
        playerScoreData.GoldenBeatData.PerfectBeats = 5;

        Sentence sentence1 = CreateDummySentence(0, 200);
        Sentence sentence2 = CreateDummySentence(201, 500);
        Sentence sentence3 = CreateDummySentence(501, 1500);

        playerScoreData.SentenceToSentenceScoreMap.Add(sentence1, CreateSentenceScore(sentence1, 3000));
        playerScoreData.SentenceToSentenceScoreMap.Add(sentence2, CreateSentenceScore(sentence2, 5000));
        playerScoreData.SentenceToSentenceScoreMap.Add(sentence3, CreateSentenceScore(sentence3, 6500));
        
        return playerScoreData;
    }

    private PartyModeSceneData CreatePartyModeSceneData()
    {
        PartyModeSceneData partyModeSceneData = new();
        partyModeSceneData.PartyModeSettings = CreatePartyModeSettings();

        // Set round index to last round
        if (isLastPartyModeRound)
        {
            partyModeSceneData.currentRoundIndex = partyModeSceneData.PartyModeSettings.roundCount - 1;
        }

        // Give team points
        for (int i = 1; i <= partyModeTeams && i < partyModeSceneData.PartyModeSettings.teamSettings.teams.Count; i++)
        {
            PartyModeTeamSettings teamSettings = partyModeSceneData.PartyModeSettings.teamSettings.teams[i];
            partyModeSceneData.teamToScoreMap[teamSettings] = i;
        }

        return partyModeSceneData;
    }

    private PartyModeSettings CreatePartyModeSettings()
    {
        PartyModeSettings partyModeSettings = new();
        partyModeSettings.roundCount = 2;
        partyModeSettings.teamSettings.isKnockOutTournament = isKnockOutTournament;
        
        void AddTeams()
        {
            for (int i = 1; i <= partyModeTeams; i++)
            {
                PlayerProfile guestPlayerProfile = new($"Guest 0{i}", EDifficulty.Medium);

                PartyModeTeamSettings teamSettings = new();
                teamSettings.name = $"Team 0{i}";
                teamSettings.guestPlayerProfiles = new List<PlayerProfile> { guestPlayerProfile };
                partyModeSettings.teamSettings.teams.Add(teamSettings);
            }
        }

        AddTeams();
        return partyModeSettings;
    }

    private Sentence CreateDummySentence(int startBeat, int endBeat)
    {
        int noteCount = 3;
        int noteLength = 10;
        Sentence sentence = new(startBeat, endBeat);
        for (int i = 0; i < noteCount; i++)
        {
            Note note = new(ENoteType.Normal, startBeat + (noteLength * i), noteLength, 0, "b");
            sentence.AddNote(note);
        }
        return sentence;
    }

    private SentenceScore CreateSentenceScore(Sentence sentence, int totalScoreSoFar)
    {
        SentenceScore sentenceScore = new(sentence);
        sentenceScore.TotalScoreSoFar = totalScoreSoFar;
        return sentenceScore;
    }
}
