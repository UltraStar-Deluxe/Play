using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GameObjectUtils
{
    /// Returns true if there is a selected GameObject and it has a Component of type InputField
    /// The EventSystem is one of the CommonSceneObjects that can be injected.
    public static bool InputFieldHasFocus(EventSystem eventSystem)
    {
        GameObject selectedGameObject = eventSystem.currentSelectedGameObject;
        return selectedGameObject != null && selectedGameObject.GetComponentInChildren<InputField>() != null;
    }

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
    // Note that this is a costly method (it searches through all Transforms and their components)
    // that should not be called frequently.
    public static T FindObjectOfType<T>(bool includeInactive) where T : MonoBehaviour
    {
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();
        if (includeInactive)
        {
            foreach (GameObject rootObject in rootObjects)
            {
                T obj = rootObject.GetComponentInChildren<T>(includeInactive);
                if (obj != null)
                {
                    return obj;
                }
            }
            Debug.LogWarning("No object of Type " + typeof(T) + " has been found in the scene.");
            return null;
        }
        else
        {
            return GameObject.FindObjectOfType<T>();
        }
    }

    // Destroys the direct children of a transform.
    public static void DestroyAllDirectChildren(this Transform transform)
    {
        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }
    }
}