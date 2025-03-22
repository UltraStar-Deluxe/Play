using System;
using System.Collections.Generic;
using System.Linq;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class JukeboxAndSingControl : MonoBehaviour, INeedInjection, IInjectionFinishedListener
{
    // Remember played songs to prefer unplayed songs in random selection.
    private readonly static HashSet<SongMeta> seenSongMetas = new HashSet<SongMeta>();

    private const float FadeTimeInSeconds = 1f;

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

    [Inject]
    private SongQueueManager songQueueManager;

    [Inject]
    private JukeboxAndSingModSettings modSettings;

    private List<VisualElement> singingUiElements = new List<VisualElement>();

    private bool isFinishing;

    private long lastMicInputTimeInMillis;

    private bool isFadedOut;
    private bool isInjectionFinished;

    public void OnInjectionFinished()
    {
        Debug.Log($"{nameof(JukeboxAndSingControl)} - OnInjectionFinished");

        isInjectionFinished = true;

        singingUiElements.AddRange(uiDocument.rootVisualElement.Query(R.UxmlNames.playerUiContainer).ToList());
        singingUiElements.AddRange(uiDocument.rootVisualElement.Query(R.UxmlNames.playerInfoContainer).ToList());
        if (modSettings.HideLyrics)
        {
            singingUiElements.AddRange(uiDocument.rootVisualElement.Query(R.UxmlNames.bottomLyricsContainer).ToList());
            singingUiElements.AddRange(uiDocument.rootVisualElement.Query(R.UxmlNames.topLyricsContainer).ToList());
        }
        
        DisableVfxCamera();

        CreateModInfoLabel();

        // Hide SingingUIElement initially. Show them after fade-out animation has finished
        singingUiElements.ForEach(elem => elem.HideByVisibility());
        AwaitableUtils.ExecuteAfterDelayInSecondsAsync(FadeTimeInSeconds, () => singingUiElements.ForEach(elem => elem.ShowByVisibility()));
        FadeOutSingingUiElements();

        // Show UI when any new notes have been recorded
        singSceneControl.PlayerControls
            .Select(playerControl => playerControl.PlayerNoteRecorder.RecordedNoteStartedEventStream)
            .Merge()
            .Where(evt => evt.RecordedNote?.TargetNote != null
                          && evt.RecordedNote.TargetNote.MidiNote == evt.RecordedNote.RoundedMidiNote)
            .Subscribe(evt => lastMicInputTimeInMillis = TimeUtils.GetUnixTimeMilliseconds());

        DisableSingSceneFinisher();
    }

    private void CreateModInfoLabel()
    {
        Label modInfoLabel = new Label();
        modInfoLabel.AddToClassList("tinyFont");
        modInfoLabel.AddToClassList("textShadow");
        modInfoLabel.text = "Jukebox&Sing mod is active";

        VisualElement songInfoContainer = uiDocument.rootVisualElement.Q("governanceOverlay").Q("songInfoContainer");
        songInfoContainer.Add(modInfoLabel);
    }

    private void Update()
    {
        if (!isInjectionFinished)
        {
            return;
        }

        UpdateSkipSong();
        UpdateUiElementsFadeOut();
        UpdateFinishingScene();
    }

    private void UpdateSkipSong()
    {
        // Skip song with button
        if (InputUtils.IsKeyboardShiftPressed()
            && Keyboard.current != null 
            && (Keyboard.current.sKey.wasReleasedThisFrame
                || Keyboard.current.rightArrowKey.wasReleasedThisFrame))
        {
            StartNextSong();
        }
    }

    private void OnDestroy()
    {
        EnableVfxCamera();
    }

    private void DisableVfxCamera()
    {
        foreach (Transform vfxCamera in Camera.main.transform)
        {
            vfxCamera.GetComponent<Camera>().enabled = false;
        }
    }

    private void EnableVfxCamera()
    {
        foreach (Transform vfxCamera in Camera.main.transform)
        {
            vfxCamera.GetComponent<Camera>().enabled = true;
        }
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
        if (songAudioPlayer.IsFullyLoaded
            && songAudioPlayer.PositionInMillis >= songAudioPlayer.DurationInMillis - timeBeforeEndInMillis)
        {
            Debug.Log($"{nameof(JukeboxAndSingControl)} - End of song detected. Starting next song soon.");
            isFinishing = true;
            float timeBeforeEndInSeconds = timeBeforeEndInMillis / 1000f;
            AwaitableUtils.ExecuteAfterDelayInSecondsAsync(timeBeforeEndInSeconds, StartNextSong);
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
        Debug.Log($"{nameof(JukeboxAndSingControl)} - Fade in singing UI elements");
        singingUiElements.ForEach(element => AnimationUtils.FadeInVisualElement(gameObject, element, 1f));
    }

    private void FadeOutSingingUiElements()
    {
        if (isFadedOut)
        {
            return;
        }

        isFadedOut = true;
        Debug.Log($"{nameof(JukeboxAndSingControl)} - Fade out singing UI elements");
        singingUiElements.ForEach(element => AnimationUtils.FadeOutVisualElement(gameObject, element, 1f));
    }

    public void StartNextSong()
    {
        SongMeta nextSongMeta = GetNextSongMeta();
        if (nextSongMeta == null)
        {
            Debug.Log($"{nameof(JukeboxAndSingControl)} - No next song found");
            return;
        }

        Debug.Log($"{nameof(JukeboxAndSingControl)} - Starting next song '{nextSongMeta.GetArtistDashTitle()}'");

        SingSceneData currentSingSceneData = SceneNavigator.GetSceneDataOrThrow<SingSceneData>();
        SingSceneData nextSingSceneData = new SingSceneData();

        nextSingSceneData.SingScenePlayerData = currentSingSceneData.SingScenePlayerData;
        nextSingSceneData.gameRoundSettings = currentSingSceneData.gameRoundSettings;
        nextSingSceneData.SongMetas = new List<SongMeta>() { nextSongMeta };
        
        sceneNavigator.LoadScene(EScene.SingScene, nextSingSceneData);
    }

    private SongMeta GetNextSongMeta()
    {
        SongMeta nextSongQueueSongMeta = GetNextSongQueueSongMeta();
        if (nextSongQueueSongMeta != null) {
            return nextSongQueueSongMeta;
        }

        return GetNextRandomSongMeta();
    }

    private SongMeta GetNextSongQueueSongMeta()
    {
        SingSceneData singSceneData = songQueueManager.CreateNextSingSceneData(singSceneControl.PartyModeSceneData);
        return singSceneData?.SongMetas?.FirstOrDefault();
    }

    private SongMeta GetNextRandomSongMeta()
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

    private void DisableSingSceneFinisher()
    {
        try
        {
            Debug.Log($"{nameof(JukeboxAndSingControl)} - Disable SingSceneFinisher");
            FindFirstObjectByType<SingSceneFinisher>().gameObject.SetActive(false);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            Debug.LogError($"{nameof(JukeboxAndSingControl)} - Failed to disable SingSceneFinisher");
        }
    }
}