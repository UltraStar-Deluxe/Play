using UniInject;
using UniRx;
using UnityEngine;

public class ApplicationStutterMonitor : AbstractSingletonBehaviour, INeedInjection
{
    public static ApplicationStutterMonitor Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<ApplicationStutterMonitor>();
    
    private const float ThresholdInMillis = 100;
    private const float ThresholdInSeconds = ThresholdInMillis / 1000f;

    [Inject]
    private SceneNavigator sceneNavigator;
    
    private float ignoreFrameDropUntilTimeInSeconds;

    private EScene currentScene;
    
    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        sceneNavigator.BeforeSceneChangeEventStream.Subscribe(_ => OnBeforeSceneChange());
        sceneNavigator.SceneChangedEventStream.Subscribe(evt => OnAfterSceneChanged(evt));
    }

    private void OnAfterSceneChanged(SceneChangedEvent evt)
    {
        currentScene = evt.NewScene;
    }

    private void OnBeforeSceneChange()
    {
        // The scene change is expected to take a bit longer
        ignoreFrameDropUntilTimeInSeconds = Time.time + 0.5f;
    }

    private void Update()
    {
        if (currentScene is EScene.SongEditorScene)
        {
            // The song editor is expected to have frame drops
            return;
        }
        
        if (Time.deltaTime > ThresholdInSeconds
            && ignoreFrameDropUntilTimeInSeconds < Time.time)
        {
            int deltaTimeInMillis = (int)(Time.deltaTime * 1000);
            Log.Debug(() => $"Frame drop detected, deltaTime: {deltaTimeInMillis} ms");
        }
    }
}
