using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class EditorUtils
{
    public static List<T> GetSelectedComponents<T>()
    {
        List<T> result = new List<T>();

        GameObject[] activeGameObjects = Selection.gameObjects;
        if (activeGameObjects == null || activeGameObjects.Length == 0)
        {
            return result;
        }

        foreach (GameObject gameObject in activeGameObjects)
        {
            T rectTransform = gameObject.GetComponent<T>();
            if (rectTransform != null)
            {
                result.Add(rectTransform);
            }
        }
        return result;
    }
}