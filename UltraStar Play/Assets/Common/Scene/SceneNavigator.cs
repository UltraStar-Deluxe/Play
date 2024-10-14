using System;
using System.Collections.Generic;
using System.Diagnostics;
using CommonOnlineMultiplayer;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SceneNavigator : AbstractSingletonBehaviour, INeedInjection
{
    public static SceneNavigator Instance => DontDestroyOnLoadManager.Instance.FindComponentOrThrow<SceneNavigator>();

    private readonly Dictionary<Type, SceneData> sceneDataTypeToSceneData = new();
    private readonly Dictionary<EScene, SceneData> sceneEnumToSceneData = new();

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

    [Inject]
    private OnlineMultiplayerManager onlineMultiplayerManager;

    public bool logSceneChangeDuration;

    public EScene CurrentScene => sceneRecipeManager.GetCurrentScene();

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
                Debug.Log($"Changing scenes took {stopwatch.ElapsedMilliseconds} ms (including animation if fade in/out transition is used)");
            }
        }).AddTo(gameObject);

        // Cannot register this in OnEnable because injection may not have finished yet in OnEnable.
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Fire initial sceneChangedEvent
        sceneChangedEventStream.OnNext(new SceneChangedEvent(ESceneUtils.GetSceneByBuildIndex(SceneManager.GetActiveScene().buildIndex)));
    }

    protected override void OnDestroySingleton()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        sceneChangedEventStream.OnNext(new SceneChangedEvent(sceneRecipeManager.GetCurrentScene()));
    }

    public void LoadScene(EScene scene, bool skipAnimation=false)
    {
        if (onlineMultiplayerManager.IsOnlineGame
            && (scene is EScene.PartyModeScene or EScene.SongEditorScene))
        {
            Debug.Log($"Cannot open {scene} when connected to online game");
            NotificationManager.CreateNotification(Translation.Get(R.Messages.onlineGame_error_notAvailable));
            return;
        }

        EScene currentScene = sceneRecipeManager.GetCurrentScene();

        beforeSceneChangeEventStream.OnNext(new BeforeSceneChangeEvent(scene, GetSceneData(scene)));

        if (SettingsUtils.ShouldAnimateSceneChange(settings))
        {
            sceneChangeAnimationControl.AnimateChangeToScene(
                () => DoChangeScene(currentScene, scene),
                () => sceneChangeAnimationControl.StartSceneChangeAnimation(currentScene, scene));
        }
        else
        {
            DoChangeScene(currentScene, scene);
        }
    }

    private void DoChangeScene(EScene currentScene, EScene targetScene)
    {
        sceneRecipeManager.UnloadScene();

        SceneRecipe sceneRecipe = sceneRecipeManager.GetSceneRecipe(targetScene);
        if (sceneRecipe != null
            && currentScene != EScene.SongEditorScene)
        {
            sceneRecipeManager.LoadSceneFromRecipe(sceneRecipe);
            sceneChangedEventStream.OnNext(new SceneChangedEvent(targetScene));
        }
        else
        {
            SceneManager.LoadSceneAsync((int)targetScene, new LoadSceneParameters(LoadSceneMode.Single, LocalPhysicsMode.None));
        }
    }

    private void AddSceneData(EScene scene, SceneData sceneData)
    {
        sceneDataTypeToSceneData[sceneData.GetType()] = sceneData;
        sceneEnumToSceneData[scene] = sceneData;
    }

    public void LoadScene(EScene scene, SceneData sceneData, bool skipAnimation=false)
    {
        if (sceneData == null)
        {
            throw new Exception("SceneData cannot be null. Use LoadScene(EScene) if no SceneData is required.");
        }
        AddSceneData(scene, sceneData);
        LoadScene(scene, skipAnimation);
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

    public static SceneData GetSceneData(EScene scene)
    {
        return Instance != null ? Instance.DoGetSceneData(scene) : null;
    }

    private SceneData DoGetSceneData(EScene scene)
    {
        if (sceneEnumToSceneData.TryGetValue(scene, out SceneData sceneData))
        {
            return sceneData;
        }
        else
        {
            return null;
        }
    }

    public static T GetSceneData<T>(T defaultValue) where T : SceneData
    {
        return Instance != null ? Instance.DoGetSceneData(defaultValue) : defaultValue;
    }

    private T DoGetSceneData<T>(T defaultValue) where T : SceneData
    {
        if (sceneDataTypeToSceneData.TryGetValue(typeof(T), out SceneData sceneData))
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
