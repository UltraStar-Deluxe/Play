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

    public void LoadScene(SceneEnumHolder holder)
    {
        LoadScene(holder.scene);
    }

    public void LoadScene(EScene scene)
    {
        SceneManager.LoadScene((int)scene);
    }
}
