using System;
using System.Collections.Generic;
using System.Diagnostics;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SceneNavigator : AbstractSingletonBehaviour, INeedInjection
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void StaticInit()
    {
        staticSceneDatas.Clear();
    }

    public static SceneNavigator Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SceneNavigator>();

    /// Static map to store and load SceneData instances across scenes.
    private static readonly Dictionary<System.Type, SceneData> staticSceneDatas = new();

    private readonly Subject<BeforeSceneChangeEvent> beforeSceneChangeEventStream = new();
    public IObservable<BeforeSceneChangeEvent> BeforeSceneChangeEventStream => beforeSceneChangeEventStream;

    private readonly Subject<SceneChangedEvent> sceneChangedEventStream = new();
    public IObservable<SceneChangedEvent> SceneChangedEventStream => sceneChangedEventStream;

    [Inject]
    private UltraStarPlaySceneChangeAnimationControl sceneChangeAnimationControl;

    [Inject]
    private Settings settings;

    [Inject]
    private SceneRecipeManager sceneRecipeManager;
    
    public bool logSceneChangeDuration;

    protected override object GetInstance()
    {
        return Instance;
    }

    protected override void StartSingleton()
    {
        Stopwatch stopwatch = new();
        BeforeSceneChangeEventStream.Subscribe(_ =>
        {
            stopwatch.Reset();
            stopwatch.Start();
        });
        SceneChangedEventStream.Subscribe(_ =>
        {
            stopwatch.Stop();
            if (logSceneChangeDuration)
            {
                Debug.Log($"Changing scenes took {stopwatch.ElapsedMilliseconds} ms");
            }
        });
    }

    protected override void OnEnableSingleton()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    protected override void OnDisableSingleton()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        sceneChangedEventStream.OnNext(new SceneChangedEvent());
    }

    public void LoadScene(EScene scene)
    {
        EScene currentScene = sceneRecipeManager.GetCurrentScene();

        beforeSceneChangeEventStream.OnNext(new BeforeSceneChangeEvent(scene));

        if (settings.GraphicSettings.AnimateSceneChange)
        {
            sceneChangeAnimationControl.AnimateChangeToScene(
                () => DoChangeScene(scene),
                () => sceneChangeAnimationControl.StartSceneChangeAnimation(currentScene, scene));
        }
        else
        {
            DoChangeScene(scene);
        }
    }

    private void DoChangeScene(EScene scene)
    {
        sceneRecipeManager.UnloadScene();
        
        SceneRecipe sceneRecipe = sceneRecipeManager.GetSceneRecipe(scene);
        if (sceneRecipe != null)
        {
            sceneRecipeManager.LoadSceneFromRecipe(sceneRecipe);
            sceneChangedEventStream.OnNext(new SceneChangedEvent());
        }
        else
        {
            SceneManager.LoadSceneAsync((int)scene, new LoadSceneParameters(LoadSceneMode.Single, LocalPhysicsMode.None));
        }
    }

    private void AddSceneData(SceneData sceneData)
    {
        staticSceneDatas[sceneData.GetType()] = sceneData;
    }

    public void LoadScene(EScene scene, SceneData sceneData)
    {
        if (sceneData == null)
        {
            throw new Exception("SceneData cannot be null. Use LoadScene(EScene) if no SceneData is required.");
        }
        AddSceneData(sceneData);
        LoadScene(scene);
    }

    public static T GetSceneDataOrThrow<T>() where T : SceneData
    {
        T sceneData = GetSceneData<T>(null);
        if (sceneData == null)
        {
            throw new SceneDataException("No SceneData found for type " + typeof(T));
        }
        return GetSceneData<T>(null);
    }

    public static T GetSceneData<T>(T defaultValue) where T : SceneData
    {
        if (staticSceneDatas.TryGetValue(typeof(T), out SceneData sceneData))
        {
            if (sceneData is T)
            {
                return sceneData as T;
            }
            else
            {
                Debug.LogError("Statically stored scene data has wrong type");
                return defaultValue;
            }
        }
        else
        {
            // Try to load default SceneData from a provider in the scene.
            // This is only for starting a scene directly inside the Unity editor with sensible defaults.
            if (Application.isEditor)
            {
                sceneData = GetDefaultSceneDataFromProvider<T>();
                if (sceneData != null)
                {
                    return sceneData as T;
                }
            }
            return defaultValue;
        }
    }

    private static T GetDefaultSceneDataFromProvider<T>() where T : SceneData
    {
        IDefaultSceneDataProvider sceneDataProvider = GameObjectUtils.FindObjectOfType<IDefaultSceneDataProvider>(false);
        if (sceneDataProvider != null)
        {
            return sceneDataProvider.GetDefaultSceneData() as T;
        }
        return null;
    }
}
