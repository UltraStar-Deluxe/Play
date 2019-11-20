using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class ExpandSceneHierarchy : MonoBehaviour
{
    // Hotkey: Alt+E
    [MenuItem("Tools/Hierarchy - Expand All In Selection &e")]
    public static void ExpandSceneHierarchyInSelectionRecursively()
    {
        GameObject[] rootObjects = Selection.gameObjects;
        List<UnityEngine.Object> newSelection = FindObjectsRecursively(rootObjects);
        Selection.objects = newSelection.ToArray();
    }

    private static List<UnityEngine.Object> FindObjectsRecursively(GameObject[] rootObjects)
    {
        List<UnityEngine.Object> resultList = new List<UnityEngine.Object>();
        foreach (GameObject rootObject in rootObjects)
        {
            FindObjectsRecursively(rootObject, resultList);
        }
        return resultList;
    }

    private static void FindObjectsRecursively(GameObject gameObject, List<UnityEngine.Object> resultList)
    {
        resultList.Add(gameObject);

        foreach (Transform child in gameObject.transform)
        {
            FindObjectsRecursively(child.gameObject, resultList);
        }
    }
}
