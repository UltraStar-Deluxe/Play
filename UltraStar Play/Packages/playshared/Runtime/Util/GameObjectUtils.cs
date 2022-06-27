using UnityEngine;
using UnityEngine.SceneManagement;

public static class GameObjectUtils
{
    /// Looks in the GameObject with the given tag
    /// for the component that is specified by the generic type parameter.
    public static T FindComponentWithTag<T>(string tag)
    {
        T component;
        GameObject obj = GameObject.FindGameObjectWithTag(tag);
        if (obj)
        {
            component = obj.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"Did not find Component '{typeof(T)}' in GameObject with tag '{tag}'.", obj);
            }
            return component;
        }

        return default(T);
    }

    // Looks for a GameObject with the given component, optionally including inactive components.
    // This method also accepts interfaces as type (in contrast to GameObject.FindObjectOfType).
    // Note that this is a costly method (it searches through all Transforms and their components)
    // that should not be called frequently.
    public static T FindObjectOfType<T>(bool includeInactive) where T : class
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        // Search the type in all components of all GameObjects.
        foreach (GameObject rootObject in rootObjects)
        {
            T obj = rootObject.GetComponentInChildren<T>(includeInactive);
            if (obj != null)
            {
                return obj;
            }
        }
        return null;
    }

    // Destroys the direct children of a transform.
    public static void DestroyAllDirectChildren(this Transform transform)
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }

    public static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
    {
        var component = gameObject.GetComponent<T>();
        if (component == null)
        {
            component = gameObject.AddComponent<T>();
        }

        return component;
    }

    public static void Destroy(UnityEngine.Object obj)
    {
        // Destroy may not be called from edit mode. Use DestroyImmediate in this case.
        if (Application.isEditor && !Application.isPlaying)
        {
            GameObject.DestroyImmediate(obj);
        }
        else
        {
            GameObject.Destroy(obj);
        }
    }
    
    public static void SetTopLevelGameObjectAndDontDestroyOnLoad(GameObject gameObject)
    {
        // Move object to top level in scene hierarchy.
        // Otherwise this object will be destroyed with its parent, even when DontDestroyOnLoad is used. 
        gameObject.transform.SetParent(null);
        GameObject.DontDestroyOnLoad(gameObject);
    }

    public static void TryInitSingleInstanceWithDontDestroyOnLoad<T>(ref T staticInstance, ref T selfInstance, bool onlyInPlayMode = true)
        where T : MonoBehaviour
    {
        if (!Application.isPlaying && onlyInPlayMode)
        {
            return;
        }

        if (staticInstance != null
            && staticInstance != selfInstance)
        {
            // This instance is not needed.
            GameObject.Destroy(selfInstance.gameObject);
            return;
        }

        staticInstance = selfInstance;

        // Move object to top level in scene hierarchy.
        // Otherwise this object will be destroyed with its parent, even when DontDestroyOnLoad is used.
        selfInstance.transform.SetParent(null);
        GameObject.DontDestroyOnLoad(selfInstance.gameObject);
    }
}
