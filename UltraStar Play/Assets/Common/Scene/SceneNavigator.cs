using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneNavigator : MonoBehaviour
{
    public static SceneNavigator Instance
    {
        get
        {
            return GameObjectUtils.FindComponentWithTag<SceneNavigator>("SceneNavigator");
        }
    }

    /// Static map to store and load SceneData instances across scenes.
    /// This static map will be reset after a Hotswap (like all static fields).
    private static readonly Dictionary<System.Type, SceneData> staticSceneDatas = new Dictionary<System.Type, SceneData>();

    public void LoadScene(SceneEnumHolder holder)
    {
        LoadScene(holder.scene);
    }

    public void LoadScene(EScene scene)
    {
        SceneManager.LoadScene((int)scene);
    }

    public void AddSceneData(SceneData sceneData)
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
            return defaultValue;
        }
    }
}
