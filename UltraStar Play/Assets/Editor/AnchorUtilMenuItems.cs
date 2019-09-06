using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// Tools to set the anchoredPosition and sizeDelta with respect to the anchor.
public class AnchorUtilMenuItems
{
    [MenuItem("Tools/Anchors (RectTransform)/Set Size To Anchors")]
    public static void SetSizeToAnchors()
    {
        SetWidthToAnchors();
        SetHeightToAnchors();
    }

    [MenuItem("Tools/Anchors (RectTransform)/Set Height to Anchors")]
    public static void SetHeightToAnchors()
    {
        GetSelection<RectTransform>().ForEach(it => it.sizeDelta = new Vector2(it.sizeDelta.x, 0));
    }

    [MenuItem("Tools/Anchors (RectTransform)/Set Width to Anchors")]
    public static void SetWidthToAnchors()
    {
        GetSelection<RectTransform>().ForEach(it => it.sizeDelta = new Vector2(0, it.sizeDelta.y));
    }

    [MenuItem("Tools/Anchors (RectTransform)/Move to Anchors")]
    public static void MoveToAnchors()
    {
        GetSelection<RectTransform>().ForEach(it => it.anchoredPosition = Vector2.zero);
    }

    [MenuItem("Tools/Anchors (RectTransform)/Set Size and Move to Anchors")]
    public static void SetSizeAndMoveToAnchors()
    {
        SetSizeToAnchors();
        MoveToAnchors();
    }

    private static List<T> GetSelection<T>()
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
