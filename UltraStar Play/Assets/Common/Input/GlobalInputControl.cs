using PrimeInputActions;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
    using UnityEditor;
#endif

public class GlobalInputControl : AbstractSingletonBehaviour, INeedInjection
{
    public static GlobalInputControl Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<GlobalInputControl>();

    [Inject]
    private Settings settings;

    [Inject]
    private VolumeControl volumeControl;
    
    [Inject]
    private SceneRecipeManager sceneRecipeManager;
    
    [Inject]
    private SceneNavigator sceneNavigator;
    
    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        RegisterInputActions();
        sceneNavigator.SceneChangedEventStream.Subscribe(_ => RegisterInputActions());
    }

    private void RegisterInputActions()
    {
        // Toggle full-screen mode via F11
        InputManager.GetInputAction(R.InputActions.usplay_toggleFullscreen).PerformedAsObservable()
            .Subscribe(_ => ToggleFullscreen());

        // Mute / unmute audio via F12
        InputManager.GetInputAction(R.InputActions.usplay_toggleMute).PerformedAsObservable()
            .Subscribe(_ => ToggleMuteAudio());
    }

    private void Update()
    {
        if (!Application.isEditor
            || Keyboard.current == null)
        {
            return;
        }
        
        if (InputUtils.IsKeyboardAltPressed()
            && Keyboard.current.rKey.wasReleasedThisFrame)
        {
            RefreshAssetDatabase();
            ReloadCurrentScene();
        }
        
        if (InputUtils.IsKeyboardControlPressed()
            && Keyboard.current.rKey.wasReleasedThisFrame)
        {
            // Refresh assets even at runtime
            RefreshAssetDatabase();
        }
    }

    private void RefreshAssetDatabase()
    {
#if UNITY_EDITOR
        AssetDatabase.Refresh();
#endif
    }
    
    private void ReloadCurrentScene()
    {
        EScene currentScene = sceneRecipeManager.GetCurrentScene();
        Debug.Log($"Reloading scene: {currentScene}");
        sceneNavigator.LoadScene(currentScene);
    }

    private void ToggleMuteAudio()
    {
        volumeControl.ToggleMuteAudio();
        if (volumeControl.IsMuted)
        {
            UiManager.CreateNotification("Mute");
        }
        else
        {
            UiManager.CreateNotification("Unmute");
        }
    }

    private void ToggleFullscreen()
    {
        Debug.Log("Toggle full-screen mode");
        Screen.fullScreen = !Screen.fullScreen;
        // A full-screen switch does not happen immediately; it will actually happen when the current frame is finished.
        StartCoroutine(CoroutineUtils.ExecuteAfterDelayInFrames(2,
            () =>
            {
                settings.FullScreenMode.Value = Screen.fullScreenMode;
                Debug.Log("New full-screen mode " + settings.FullScreenMode.Value);
            }));
    }
}
