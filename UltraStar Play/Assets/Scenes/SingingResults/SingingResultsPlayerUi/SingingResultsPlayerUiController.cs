using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingingResultsPlayerUiController : MonoBehaviour
{

    public void Init(int playerProfileIndex,
        PlayerProfile playerProfile,
        SingingResultsSceneData.PlayerScoreResultData playerScoreData,
        MicProfile micProfile)
    {
        SetPlayerProfile(playerProfileIndex, playerProfile, micProfile);
        SetNormalNotesScore(playerScoreData.NormalNotesScore);
        SetGoldenNotesScore(playerScoreData.GoldenNotesScore);
        SetPerfectSentenceBonusScore(playerScoreData.PerfectSentenceBonusScore);
        SetTotalScore(playerScoreData.TotalScore);
    }

    private void SetTotalScore(double totalScore)
    {
        SongRating songRating = GetSongRating(totalScore);
        GetComponentInChildren<SongRatingText>().SetSongRating(songRating);
        GetComponentInChildren<SongRatingImage>().SetSongRating(songRating);
        GetComponentInChildren<SingingResultsTotalScoreText>().TargetValue = totalScore;
        GetComponentInChildren<SingingResultsScoreBar>().TargetValue = totalScore;
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

    private void SetPerfectSentenceBonusScore(double score)
    {
        GetComponentInChildren<PerfectSentenceBonusScoreText>().TargetValue = score;
    }

    private void SetGoldenNotesScore(double score)
    {
        GetComponentInChildren<GoldenNotesScoreText>().TargetValue = score;
    }

    private void SetNormalNotesScore(double score)
    {
        GetComponentInChildren<NormalNotesScoreText>().TargetValue = score;
    }

    private void SetPlayerProfile(int playerProfileIndex, PlayerProfile playerProfile, MicProfile micProfile)
    {
        GetComponentInChildren<PlayerNameText>().SetText(playerProfile.Name);
        GetComponentInChildren<AvatarImage>().SetPlayerProfile(playerProfile);
        GetComponentInChildren<PlayerIndexText>().SetPlayerProfileIndex(playerProfileIndex);
        if (micProfile != null)
        {
            GetComponentInChildren<AvatarImage>().SetColor(micProfile.Color);
            GetComponentInChildren<PlayerIndexText>().SetBackgroundImageColor(micProfile.Color);
        }
    }
}
