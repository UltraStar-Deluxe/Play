using System.Collections.Generic;
using UnityEngine;

public class DefaultSingingResultsSceneDataProvider : MonoBehaviour, IDefaultSceneDataProvider
{
    [Range(1, 16)]
    public int playerCount = 1;

    [Range(0, 8)]
    public int partyModeTeams = 5;

    public string songTitle;

    public Vector2 scoreRange = new Vector2(2000, 8000);

    public bool isLastPartyModeRound;
    public bool isKnockOutTournament;

    public SceneData GetDefaultSceneData()
    {
        SingingResultsSceneData data = new();

        SongMetaManager.Instance.WaitUntilSongScanFinished();

        Settings settings = SettingsManager.Instance.Settings;

        SongMeta songMeta = SongMetaManager.Instance.GetSongMetaByTitle(songTitle);
        if (songMeta == null)
        {
            Debug.LogError($"Did not find song meta with title: {songTitle}, using first found song instead.");
            songMeta = SongMetaManager.Instance.GetFirstSongMeta();
        }

        data.SongMetas = new List<SongMeta> { songMeta };
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
            partyModeSceneData.currentRoundIndex = partyModeSceneData.PartyModeSettings.RoundCount - 1;
        }

        // Give team points
        for (int i = 0; i <= partyModeTeams && i < partyModeSceneData.PartyModeSettings.TeamSettings.Teams.Count; i++)
        {
            PartyModeTeamSettings teamSettings = partyModeSceneData.PartyModeSettings.TeamSettings.Teams[i];
            partyModeSceneData.teamToScoreMap[teamSettings] = (i + 2);
        }

        return partyModeSceneData;
    }

    private PartyModeSettings CreatePartyModeSettings()
    {
        PartyModeSettings partyModeSettings = new();
        partyModeSettings.RoundCount = 2;
        partyModeSettings.TeamSettings.IsKnockOutTournament = isKnockOutTournament;

        void AddTeams()
        {
            for (int i = 1; i <= partyModeTeams; i++)
            {
                PlayerProfile guestPlayerProfile = new($"Guest 0{i}", EDifficulty.Medium);

                PartyModeTeamSettings teamSettings = new();
                if (i == 1)
                {
                    teamSettings.name = "Sonic Sensations";
                }
                else if (i == 2)
                {
                    teamSettings.name = "Dazzling Divas";
                }
                else if (i == 3)
                {
                    teamSettings.name = "Hyper Harmonics";
                }
                else
                {
                    teamSettings.name = $"Team 0{i}";
                }

                teamSettings.guestPlayerProfiles = new List<PlayerProfile> { guestPlayerProfile };
                partyModeSettings.TeamSettings.Teams.Add(teamSettings);
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
