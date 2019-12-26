using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeBarTimeLine : MonoBehaviour
{
    public TimeBarTimeLineRect timeLineRectPrefab;

    public void Init(SongMeta songMeta, List<PlayerController> playerControllers, double durationOfSongInMillis)
    {
        foreach (TimeBarTimeLineRect timeLineRect in GetComponentsInChildren<TimeBarTimeLineRect>())
        {
            Destroy(timeLineRect.gameObject);
        }

        foreach (PlayerController playerController in playerControllers)
        {
            CreateTimeLineRects(songMeta, playerController, durationOfSongInMillis);
        }
    }

    private void CreateTimeLineRects(SongMeta songMeta, PlayerController playerController, double durationOfSongInMillis)
    {
        foreach (Sentence sentence in playerController.Voice.Sentences)
        {
            double startPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, sentence.MinBeat);
            double endPosInMillis = BpmUtils.BeatToMillisecondsInSong(songMeta, sentence.MaxBeat);
            PlayerProfile playerProfile = playerController.PlayerProfile;
            MicProfile micProfile = playerController.MicProfile;
            CreateTimeLineRect(playerProfile, micProfile, startPosInMillis, endPosInMillis, durationOfSongInMillis);
        }
    }

    private void CreateTimeLineRect(PlayerProfile playerProfile, MicProfile micProfile, double startPosInMillis, double endPosInMillis, double durationOfSongInMillis)
    {
        double startPosPercentage = startPosInMillis / durationOfSongInMillis;
        double endPosPercentage = endPosInMillis / durationOfSongInMillis;

        TimeBarTimeLineRect timeLineRect = Instantiate(timeLineRectPrefab);
        timeLineRect.transform.SetParent(transform);
        timeLineRect.transform.SetSiblingIndex(0);
        RectTransform rectTransform = timeLineRect.GetComponent<RectTransform>();

        // Position in parent relatively to the position of start and end in the overall duration.
        rectTransform.anchorMin = new Vector2((float)startPosPercentage, rectTransform.anchorMin.y);
        rectTransform.anchorMax = new Vector2((float)endPosPercentage, rectTransform.anchorMax.y);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        // Set color of rect to color of mic.
        if (micProfile != null)
        {
            timeLineRect.SetColor(micProfile.Color);
        }
    }
}
