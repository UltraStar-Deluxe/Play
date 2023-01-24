using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UniInject;
using UniRx;
using UnityEngine.SceneManagement;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class BackgroundMusicManager : AbstractSingletonBehaviour, INeedInjection
{
    public static BackgroundMusicManager Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<BackgroundMusicManager>();

    private static readonly int timeInSecondsBeforeRestartingBackgroundMusic = 20;
    private static readonly List<EScene> scenesWithoutBackgroundMusic = new()
    {
        EScene.SingScene,
        EScene.SongSelectScene,
        EScene.SingingResultsScene,
        EScene.HighscoreScene,
        EScene.SongEditorScene,
        EScene.RecordingOptionsScene,
    };

    private bool ShouldPlayBackgroundMusic
    {
        get
        {
            if (settings.AudioSettings.BackgroundMusicVolumePercent <= 0)
            {
                return false;
            }

            EScene sceneEnum = ESceneUtils.GetSceneByBuildIndex(SceneManager.GetActiveScene().buildIndex);
            return !scenesWithoutBackgroundMusic.Contains(sceneEnum);
        }
    }

    [Inject(SearchMethod = SearchMethods.GetComponentInChildren)]
    private AudioSource backgroundMusicAudioSource;

    [Inject]
    private Settings settings;

    [Inject]
    private SceneNavigator sceneNavigator;

    private float lastPauseTimeInSeconds;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        settings.ObserveEveryValueChanged(it => it.AudioSettings.BackgroundMusicVolumePercent)
            .Subscribe(_ => UpdateBackgroundMusic())
            .AddTo(gameObject);
        sceneNavigator.SceneChangedEventStream.Subscribe(_ => UpdateBackgroundMusic());
    }

    private void UpdateBackgroundMusic()
    {
        // Update volume
        backgroundMusicAudioSource.volume = settings.AudioSettings.BackgroundMusicVolumePercent / 100f;

        // Play or pause the music
        if (ShouldPlayBackgroundMusic
            && !backgroundMusicAudioSource.isPlaying)
        {
            // If the music did not play for a longer duration, then start it from the beginning.
            float timeInSecondsWithoutBackgroundMusic = Time.time - lastPauseTimeInSeconds;
            if (lastPauseTimeInSeconds > 0
                && timeInSecondsWithoutBackgroundMusic > timeInSecondsBeforeRestartingBackgroundMusic)
            {
                Debug.Log($"Did not play background music for {timeInSecondsWithoutBackgroundMusic} seconds. Restarting it from the beginning.");
                backgroundMusicAudioSource.Stop();
            }
            backgroundMusicAudioSource.Play();
        }
        else if (!ShouldPlayBackgroundMusic
            && backgroundMusicAudioSource.isPlaying)
        {
            backgroundMusicAudioSource.Pause();
            lastPauseTimeInSeconds = Time.time;
        }
    }
}
