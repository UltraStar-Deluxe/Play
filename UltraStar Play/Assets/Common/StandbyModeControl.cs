using UniInject;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.Video;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class StandbyModeControl : AbstractSingletonBehaviour, INeedInjection
{
    public static StandbyModeControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<StandbyModeControl>();

    [Inject]
    private ApplicationManager applicationManager;

    [Inject]
    private Settings settings;

    [Inject]
    private ThemeManager themeManager;

    [Inject]
    private UIDocument uiDocument;

    private bool standby;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected void Update()
    {
        EKeyboardModifier currentKeyboardModifier = InputUtils.GetCurrentKeyboardModifier();
        if (currentKeyboardModifier is EKeyboardModifier.CtrlShiftAlt
            && Keyboard.current != null
            && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            ToggleStandbyMode();
        }
    }

    private void OnGUI()
    {
        if (standby)
        {
            if (GUI.Button(new Rect(10, 10, 200, 100), "Exit standby mode of \nMelody Mania"))
            {
                DisableStandbyMode();
            }
        }
    }

    private void ToggleStandbyMode()
    {
        if (standby)
        {
            DisableStandbyMode();
        }
        else
        {
            EnableStandbyMode();
        }
    }

    private void EnableStandbyMode()
    {
        Debug.Log("Enter stand-by mode");
        standby = true;
        Time.timeScale = 0;

        // Reduce target FPS
        applicationManager.targetFrameRate = 2;

        // Lower resolution
        if (!Application.isEditor)
        {
            Screen.SetResolution(480, 270, Screen.fullScreenMode, Screen.currentResolution.refreshRate);
        }

        // Pause audio
        FindObjectsOfType<SongAudioPlayer>().ForEach(it => it.PauseAudio());
        FindObjectsOfType<AudioSource>().ForEach(it => it.Pause());

        // Pause video
        FindObjectsOfType<VideoPlayer>().ForEach(it => it.Pause());

        // Hide UI
        uiDocument.rootVisualElement.HideByDisplay();
        settings.AnimatedBackground = false;

        // Update theme with changed settings
        themeManager.SetCurrentTheme(themeManager.GetCurrentTheme());
    }

    private void DisableStandbyMode()
    {
        Debug.Log("Exit stand-by mode");
        standby = false;
        Time.timeScale = 1;

        // Reset target FPS
        applicationManager.targetFrameRate = settings.TargetFps;

        // Reset resolution
        if (!Application.isEditor)
        {
            Screen.SetResolution(
                settings.ScreenResolution.Width,
                settings.ScreenResolution.Height,
                settings.FullScreenMode.ToUnityFullScreenMode(),
                settings.ScreenResolution.RefreshRate);
        }

        // Reset audio
        FindObjectsOfType<SongAudioPlayer>().ForEach(it => it.PlayAudio());
        FindObjectsOfType<AudioSource>().ForEach(it =>
        {
            if (it.clip != null
                && it.clip.length > 0)
            {
                it.Play();
            }
        });

        // Reset video
        FindObjectsOfType<VideoPlayer>().ForEach(it =>
        {
            if (!it.url.IsNullOrEmpty()
                && it.length > 0)
            {
                it.Play();
            }
        });

        // Reset UI
        uiDocument.rootVisualElement.ShowByDisplay();
        settings.AnimatedBackground = true;

        // Update theme with changed settings
        themeManager.SetCurrentTheme(themeManager.GetCurrentTheme());
    }
}
