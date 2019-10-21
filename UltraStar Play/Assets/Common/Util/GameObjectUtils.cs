using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
    // Note that this is a costly method that should not be called frequently.
    public static T FindObjectOfType<T>(bool includeInactive) where T : MonoBehaviour
    {
        // The current implementation of finding the root transforms is costly.
        List<Transform> rootTransforms = GameObject.FindObjectsOfType<Transform>().Where(it => it.root == it).ToList();
        if (includeInactive)
        {
            foreach (Transform rootTransform in rootTransforms)
            {
                T obj = rootTransform.GetComponentInChildren<T>(true);
                if (obj != null)
                {
                    return obj;
                }
            }
            return null;
        }
        else
        {
            return GameObject.FindObjectOfType<T>();
        }
    }
}