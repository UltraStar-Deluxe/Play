using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DefaultSingingResultsSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    [Range(1, 16)]
    public int playerCount = 1;

    [Range(0, 8)]
    public int partyModeTeams = 5;

    public bool isLastPartyModeRound;
    
    public SceneData GetDefaultSceneData()
    {
        SingingResultsSceneData data = new();

        SongMetaManager.Instance.WaitUntilSongScanFinished();
        data.SongMetas = new List<SongMeta> { SongMetaManager.Instance.GetFirstSongMeta() };
        data.SongDurationInMillis = 120 * 1000;

        PlayerScoreControlData playerScoreData = new();
        playerScoreData.TotalScore = 6500;
        playerScoreData.NormalNotesTotalScore = 4000;
        playerScoreData.GoldenNotesTotalScore = 2000;
        playerScoreData.PerfectSentenceBonusTotalScore = 500;

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

        List<PlayerProfile> settingsPlayerProfiles = SettingsManager.Instance.Settings.PlayerProfiles;
        PlayerProfile playerProfile = settingsPlayerProfiles[0];
        data.PlayerProfileToMicProfileMap[playerProfile] = SettingsManager.Instance.Settings.MicProfiles.FirstOrDefault();
        data.AddPlayerScores(playerProfile, playerScoreData);
        for (int i = 1; i < playerCount && i < settingsPlayerProfiles.Count; i++)
        {
            data.AddPlayerScores(settingsPlayerProfiles[i], playerScoreData);
        }

        if (partyModeTeams > 0)
        {
            data.partyModeSceneData = CreatePartyModeSceneData();
        }
        return data;
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
