using System;
using System.Collections;
using System.Collections.Generic;
using SceneChangeAnimations;
using UniInject;
using UniRx;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Disable warning about fields that are never assigned, their values are injected.
#pragma warning disable CS0649

public class SceneNavigator : MonoBehaviour, INeedInjection
{
    private readonly Subject<BeforeSceneChangeEvent> beforeSceneChangeEventStream = new();
    public IObservable<BeforeSceneChangeEvent> BeforeSceneChangeEventStream => beforeSceneChangeEventStream;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        staticSceneDatas.Clear();
    }

    [Inject]
    private UltraStarPlaySceneChangeAnimationControl sceneChangeAnimationControl;

    public static SceneNavigator Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<SceneNavigator>("SceneNavigator");
        }
    }

    /// Static map to store and load SceneData instances across scenes.
    private static readonly Dictionary<System.Type, SceneData> staticSceneDatas = new();

    public void LoadScene(SceneEnumHolder holder)
    {
        LoadScene(holder.scene);
    }

    public void LoadScene(EScene scene)
    {
        beforeSceneChangeEventStream.OnNext(new BeforeSceneChangeEvent(scene));

        sceneChangeAnimationControl.AnimateChangeToScene(
            () => SceneManager.LoadScene((int)scene),
            sceneChangeAnimationControl.StartSceneChangeAnimation);
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

    public T GetSceneDataOrThrow<T>() where T : SceneData
    {
        T sceneData = GetSceneData<T>(null);
        if (sceneData == null)
        {
            throw new SceneDataException("No SceneData found for type " + typeof(T));
        }
        return GetSceneData<T>(null);
    }

    public T GetSceneData<T>(T defaultValue) where T : SceneData
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

    private T GetDefaultSceneDataFromProvider<T>() where T : SceneData
    {
        IDefaultSceneDataProvider sceneDataProvider = GameObjectUtils.FindObjectOfType<IDefaultSceneDataProvider>(false);
        if (sceneDataProvider != null)
        {
            return sceneDataProvider.GetDefaultSceneData() as T;
        }
        return null;
    }
}
