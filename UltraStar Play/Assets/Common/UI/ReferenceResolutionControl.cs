using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class ReferenceResolutionControl : AbstractSingletonBehaviour, INeedInjection
{
    private static readonly Vector2Int referenceResolution = new Vector2Int(1024, 768);
    public static ReferenceResolutionControl Instance => DontDestroyOnLoadManager.FindComponentOrThrow<ReferenceResolutionControl>();

    [Inject]
    private Settings settings;
    
    [Inject]
    private UIDocument uiDocument;
    
    [Inject]
    private SceneNavigator sceneNavigator;
    
    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        settings.ObserveEveryValueChanged(it => it.ReferenceResolutionScaleFactor)
            .Subscribe(_ => ApplyReferenceResolutionScaleFactor())
            .AddTo(gameObject);
        
        settings.ObserveEveryValueChanged(it => it.SongEditorSettings.ReferenceResolutionScaleFactor)
            .Subscribe(_ => ApplyReferenceResolutionScaleFactor())
            .AddTo(gameObject);
        
        sceneNavigator.SceneChangedEventStream
            .Subscribe(_ =>
            {
                ApplyReferenceResolutionScaleFactor();
            })
            .AddTo(gameObject);
    }

    protected override void OnDestroySingleton()
    {
        Reset();
    }

    private void Reset()
    {
        settings.ReferenceResolutionScaleFactor = 1;
        settings.SongEditorSettings.ReferenceResolutionScaleFactor = 1;
        uiDocument.panelSettings.referenceResolution = referenceResolution;
    }

    private void ApplyReferenceResolutionScaleFactor()
    {
        double newValue = sceneNavigator.CurrentScene is EScene.SongEditorScene
            ? settings.SongEditorSettings.ReferenceResolutionScaleFactor
            : settings.ReferenceResolutionScaleFactor;
        newValue = NumberUtils.Limit(newValue, 0.5, 2);

        // Increasing the reference resolution makes the ui smaller. So divide instead of multiply.
        uiDocument.panelSettings.referenceResolution = new Vector2Int(
            (int)(referenceResolution.x / newValue),
            (int)(referenceResolution.y / newValue));
    }
}
