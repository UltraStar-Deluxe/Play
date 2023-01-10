using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SingSceneMedleyControl : INeedInjection
{
    private const int CountDownTimeInSeconds = 9;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SingSceneCountdownControl countdownControl;

    [Inject]
    private SingSceneAudioFadeInControl audioFadeInControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SceneNavigator sceneNavigator;

    private bool isSongFinished;
    private float durationAfterSongFinishedInSeconds;

    public void Update()
    {
        if (!isSongFinished
            && Math.Abs(songAudioPlayer.PositionInSongInMillis - GetMedleyEndInMillis()) < 1000)
        {
            isSongFinished = true;
        }
        else if (isSongFinished)
        {
            durationAfterSongFinishedInSeconds += Time.deltaTime;
            if (durationAfterSongFinishedInSeconds >= 2)
            {
                FinishMedleySong();
            }
        }
    }

    private void FinishMedleySong()
    {
        SingSceneData singSceneData = GetSingSceneData();
        if (singSceneData.MedleySongIndex >= singSceneData.SongMetas.Count - 1)
        {
            singSceneControl.FinishScene(false);
        }
        else
        {
            SingSceneData newSingSceneData = new(singSceneData);
            newSingSceneData.MedleySongIndex++;
            sceneNavigator.LoadScene(EScene.SingScene, newSingSceneData, true);
        }
    }

    public void StartCurrentMedleySong()
    {
        Debug.Log($"Starting current medley song '{SongMetaUtils.GetArtistDashTitle(GetCurrentMedleySongMeta())}'");
        singSceneControl.SkipToPositionInSong(GetMedleyStartWithCountdownInMillis());
        countdownControl.StartCountdown(CountDownTimeInSeconds);
        audioFadeInControl.StartAudioFadeIn(CountDownTimeInSeconds / 2);
    }

    public double GetMedleyStartWithCountdownInMillis()
    {
        return NumberUtils.Limit(GetMedleyStartWithoutCountdownInMillis() - CountDownTimeInSeconds * 1000, 0, double.MaxValue);
    }

    public double GetMedleyStartWithoutCountdownInMillis()
    {
        SongMeta songMeta = GetCurrentMedleySongMeta();
        int medleyStartBeat = SongMetaUtils.GetMedleyStartBeat(songMeta);
        return NumberUtils.Limit(BpmUtils.BeatToMillisecondsInSong(songMeta, medleyStartBeat), 0, songAudioPlayer.DurationOfSongInMillis);
    }

    public double GetMedleyEndInMillis()
    {
        SongMeta songMeta = GetCurrentMedleySongMeta();
        int medleyEndBeat = SongMetaUtils.GetMedleyEndBeat(songMeta);
        return NumberUtils.Limit(BpmUtils.BeatToMillisecondsInSong(songMeta, medleyEndBeat), 0, songAudioPlayer.DurationOfSongInMillis);
    }

    public double GetMedleyDurationWithCountdownInMillis()
    {
        return GetMedleyEndInMillis() - GetMedleyStartWithCountdownInMillis();
    }

    private SongMeta GetCurrentMedleySongMeta()
    {
        return GetSingSceneData().SongMetas[GetSingSceneData().MedleySongIndex];
    }

    private SingSceneData GetSingSceneData()
    {
        return singSceneControl.SceneData;
    }
}
