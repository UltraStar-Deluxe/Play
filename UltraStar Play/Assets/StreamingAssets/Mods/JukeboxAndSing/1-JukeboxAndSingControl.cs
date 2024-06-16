using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class JukeboxAndSingControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    // Remember played songs to prefer unplayed songs in random selection.
    private readonly static HashSet<SongMeta> seenSongMetas = new HashSet<SongMeta>();

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    [Inject]
    private SceneNavigator sceneNavigator;

    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private UIDocument uiDocument;

    private List<VisualElement> singingUiElements = new List<VisualElement>();

    private bool isFinishing;

    private long lastMicInputTimeInMillis;

    private bool isFadedOut;
    private bool isInjectionFinished;

    public void OnInjectionFinished()
    {
        Debug.Log("nameof(JukeboxAndSingControl)} -  OnInjectionFinished");

        isInjectionFinished = true;
        singingUiElements.AddRange(uiDocument.rootVisualElement.Query(R.UxmlNames.playerUiContainer).ToList());
        singingUiElements.AddRange(uiDocument.rootVisualElement.Query(R.UxmlNames.bottomLyricsContainer).ToList());

        FadeOutSingingUiElements();

        // Show UI when any new notes have been recorded
        singSceneControl.PlayerControls
            .Select(playerControl => playerControl.PlayerNoteRecorder.RecordedNoteStartedEventStream)
            .Merge()
            .Where(evt => evt.RecordedNote?.TargetNote != null
                          && evt.RecordedNote.TargetNote.MidiNote == evt.RecordedNote.RoundedMidiNote)
            .Subscribe(evt => lastMicInputTimeInMillis = TimeUtils.GetUnixTimeMilliseconds());
    }

    private void Update()
    {
        if (!isInjectionFinished)
        {
            return;
        }

        UpdateUiElementsFadeOut();
        UpdateFinishingScene();
    }

    private void UpdateUiElementsFadeOut()
    {
        if (lastMicInputTimeInMillis <= 0)
        {
            return;
        }
        
        if (TimeUtils.IsDurationAboveThresholdInMillis(lastMicInputTimeInMillis, 10000))
        {
            FadeOutSingingUiElements();
        }
        else
        {
            FadeInSingingUiElements();
        }
    }

    private void UpdateFinishingScene()
    {
        if (isFinishing)
        {
            return;
        }

        int timeBeforeEndInMillis = 1000;
        if (songAudioPlayer.PositionInMillis >= songAudioPlayer.DurationInMillis - timeBeforeEndInMillis)
        {
            Debug.Log($"{nameof(JukeboxAndSingControl)} - End of song detected. Starting next song soon.");
            isFinishing = true;
            float timeBeforeEndInSeconds = timeBeforeEndInMillis / 1000f;
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(timeBeforeEndInSeconds, StartNextSong));
            return;
        }
    }

    private void FadeInSingingUiElements()
    {
        if (!isFadedOut)
        {
            return;
        }

        isFadedOut = false;
        Debug.Log($"{nameof(JukeboxAndSingControl)} -  Fade in singing UI elements");
        singingUiElements.ForEach(element => AnimationUtils.FadeInVisualElement(gameObject, element, 1f));
    }

    private void FadeOutSingingUiElements()
    {
        if (isFadedOut)
        {
            return;
        }

        isFadedOut = true;
        Debug.Log($"{nameof(JukeboxAndSingControl)} -  Fade out singing UI elements");
        singingUiElements.ForEach(element => AnimationUtils.FadeOutVisualElement(gameObject, element, 1f));
    }

    public void StartNextSong()
    {
        SongMeta nextSongMeta = GetNextSongMeta();
        if (nextSongMeta == null)
        {
            Debug.Log($"{nameof(JukeboxAndSingControl)} -  no next song found");
            return;
        }

        Debug.Log($"{nameof(JukeboxAndSingControl)} -  starting next song '{SongMetaUtils.GetArtistDashTitle(nextSongMeta)}'");

        SingSceneData currentSingSceneData = SceneNavigator.GetSceneDataOrThrow<SingSceneData>();
        SingSceneData nextSingSceneData = new SingSceneData();

        nextSingSceneData.SingScenePlayerData = currentSingSceneData.SingScenePlayerData;
        nextSingSceneData.gameRoundSettings = currentSingSceneData.gameRoundSettings;
        nextSingSceneData.SongMetas = new List<SongMeta>() { nextSongMeta };
        
        sceneNavigator.LoadScene(EScene.SingScene, nextSingSceneData);
    }

    private SongMeta GetNextSongMeta()
    {
        List<SongMeta> unseenSongMetas = songMetaManager.GetSongMetas()
            .Except(seenSongMetas)
            .ToList();

        if (unseenSongMetas.IsNullOrEmpty())
        {
            seenSongMetas.Clear();
            unseenSongMetas = songMetaManager.GetSongMetas().ToList();
        }
        return RandomUtils.RandomOf(unseenSongMetas);
    }
}