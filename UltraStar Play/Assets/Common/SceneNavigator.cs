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
            var obj = GameObject.FindGameObjectWithTag("SceneNavigator");
            if (obj)
            {
                return obj.GetComponent<SceneNavigator>();
            }
            else
            {
                Debug.LogError("Cannot find instance");
                return null;
            }
        }
    }

    /// Static map to store and load SceneData instances across scenes.
    /// This static map will be reset after a Hotswap (like all static fields).
    private static Dictionary<System.Type, SceneData> staticSceneDatas = new Dictionary<System.Type, SceneData>();

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
        AddSceneData(sceneData);
        SceneManager.LoadScene((int)scene);
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
