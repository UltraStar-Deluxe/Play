using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UniInject;
using UniRx;

public class GlobalKeyboardShortcutManager : MonoBehaviour
{
    void Update()
    {
        // Toggle full-screen mode via F11
        if (Input.GetKeyUp(KeyCode.F11))
        {
            Debug.Log("Toggle full-screen mode");
            Screen.fullScreen = !Screen.fullScreen;
            // Screen.fullScreenMode is updated after this frame. Thus, delay updating the settings a bit.
            StartCoroutine(CoroutineUtils.ExecuteAfterDelayInSeconds(0.1f,
                () => SettingsManager.Instance.Settings.GraphicSettings.fullScreenMode = Screen.fullScreenMode));
        }

        // Mute / unmute audio via F12
        if (Input.GetKeyUp(KeyCode.F12))
        {
            AudioManager.ToggleMuteAudio();
        }
    }
}
