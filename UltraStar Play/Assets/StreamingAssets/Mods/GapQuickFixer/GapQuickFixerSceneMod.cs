using System;
using UniInject;
using UnityEngine;
using UnityEngine.InputSystem;

public class GapQuickFixerSceneMod : ISceneMod
{
    public void OnSceneEntered(SceneEnteredContext sceneEnteredContext)
    {
        if (sceneEnteredContext.Scene == EScene.SingScene)
        {
            GameObject gameObject = new GameObject();
            gameObject.name = nameof(GapQuickFixerMonoBehaviour);
            GapQuickFixerMonoBehaviour behaviour = gameObject.AddComponent<GapQuickFixerMonoBehaviour>();
            sceneEnteredContext.SceneInjector.Inject(behaviour);
        }
    }
}

public class GapQuickFixerMonoBehaviour : MonoBehaviour, INeedInjection
{
    [Inject]
    private SongMetaManager songMetaManager;

    [Inject]
    private SingSceneControl singSceneControl;

    [Inject]
    private SongAudioPlayer songAudioPlayer;

    // Update is called once per frame
    private void Update()
    {
        if (WasSetGapShortcutTriggered())
        {
            SetGapThenSaveSong();
        }
    }

    private void SetGapThenSaveSong()
    {
        SongMeta songMeta = singSceneControl.SongMeta;

        // Set GAP
        double newGapInMillis = songAudioPlayer.PositionInMillis;
        double newGapInSeconds = newGapInMillis / 1000;
        songMeta.GapInMillis = newGapInMillis;
        NotificationManager.CreateNotification(Translation.Of($"Set GAP to {newGapInSeconds:0.00} s"));

        // Save song
        songMetaManager.SaveSong(songMeta, false);
    }

    private bool WasSetGapShortcutTriggered()
    {
        return Keyboard.current != null
            && Keyboard.current.ctrlKey.IsPressed()
            && Keyboard.current.gKey.wasPressedThisFrame;
    }
}