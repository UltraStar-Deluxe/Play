using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;
 
public class GlobalInputControl : MonoBehaviour
{
    private void Start()
    {
        // Toggle full-screen mode via F11
        InputManager.GetInputAction(R.InputActions.usplay_toggleFullscreen).PerformedAsObservable()
            .Subscribe(_ => ToggleFullscreen());

        // Mute / unmute audio via F12
        InputManager.GetInputAction(R.InputActions.usplay_toggleMute).PerformedAsObservable()
            .Subscribe(_ => AudioManager.ToggleMuteAudio());
    }

    private void ToggleFullscreen()
    {
        Debug.Log("Toggle full-screen mode");
        Screen.fullScreen = !Screen.fullScreen;
        // Screen.fullScreenMode is updated after this frame. Thus, delay updating the settings a bit.
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(0.1f,
            () => SettingsManager.Instance.Settings.GraphicSettings.fullScreenMode = Screen.fullScreenMode));
    }
}
