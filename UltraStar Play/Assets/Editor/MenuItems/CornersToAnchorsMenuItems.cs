using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CornersToAnchorsMenuItems
{

    // Hotkey: Alt+C
    [MenuItem("Tools/Corners to Anchors (RectTransform)/Width and Height and Position &c")]
    public static void MoveCornersToAnchors_WidthAndHeightAndPosition()
    {
        EditorUtils.GetSelectedComponents<RectTransform>().ForEach(it =>
        {
            Undo.RecordObject(it, "MoveCornersToAnchors_WidthAndHeightAndPosition");
            MoveCornersToAnchorsExtensions.MoveCornersToAnchors_Width(it);
            MoveCornersToAnchorsExtensions.MoveCornersToAnchors_Height(it);
            MoveCornersToAnchorsExtensions.MoveCornersToAnchors_CenterPosition(it);
        });
    }

    [MenuItem("Tools/Corners to Anchors (RectTransform)/Width and Height")]
    public static void MoveCornersToAnchors_WidthAndHeight()
    {
        EditorUtils.GetSelectedComponents<RectTransform>().ForEach(it =>
        {
            Undo.RecordObject(it, "MoveCornersToAnchors_WidthAndHeight");
            MoveCornersToAnchorsExtensions.MoveCornersToAnchors_Width(it);
            MoveCornersToAnchorsExtensions.MoveCornersToAnchors_Height(it);
        });
    }

    [MenuItem("Tools/Corners to Anchors (RectTransform)/Width")]
    public static void MoveCornersToAnchors_Width()
    {
        EditorUtils.GetSelectedComponents<RectTransform>().ForEach(it =>
        {
            Undo.RecordObject(it, "MoveCornersToAnchors_Width");
            MoveCornersToAnchorsExtensions.MoveCornersToAnchors_Width(it);
        });
    }

    [MenuItem("Tools/Corners to Anchors (RectTransform)/Width and Center Horizontal")]
    public static void MoveCornersToAnchors_WidthAndCenterHorizontal()
    {
        EditorUtils.GetSelectedComponents<RectTransform>().ForEach(it =>
        {
            Undo.RecordObject(it, "MoveCornersToAnchors_WidthAndCenterHorizontal");
            MoveCornersToAnchorsExtensions.MoveCornersToAnchors_Width(it);
            it.anchoredPosition = new Vector2(0, it.anchoredPosition.y);
        });
    }

    [MenuItem("Tools/Corners to Anchors (RectTransform)/Height")]
    public static void MoveCornersToAnchors_Height()
    {
        EditorUtils.GetSelectedComponents<RectTransform>().ForEach(it =>
        {
            Undo.RecordObject(it, "MoveCornersToAnchors_Height");
            MoveCornersToAnchorsExtensions.MoveCornersToAnchors_Height(it);
        });
    }

    [MenuItem("Tools/Corners to Anchors (RectTransform)/Height and Center Vertical")]
    public static void MoveCornersToAnchors_HeightAndCenterVertical()
    {
        EditorUtils.GetSelectedComponents<RectTransform>().ForEach(it =>
        {
            Undo.RecordObject(it, "MoveCornersToAnchors_HeightAndCenterVertical");
            MoveCornersToAnchorsExtensions.MoveCornersToAnchors_Height(it);
            it.anchoredPosition = new Vector2(it.anchoredPosition.x, 0);
        });
    }

    [MenuItem("Tools/Corners to Anchors (RectTransform)/Center Horizontal and Vertical")]
    public static void MoveCornersToAnchors_CenterPosition()
    {
        EditorUtils.GetSelectedComponents<RectTransform>().ForEach(it =>
        {
            Undo.RecordObject(it, "MoveCornersToAnchors_CenterPosition");
            MoveCornersToAnchorsExtensions.MoveCornersToAnchors_CenterPosition(it);
        });
    }

    [MenuItem("Tools/Corners to Anchors (RectTransform)/Center Horizontal")]
    public static void MoveCornersToAnchors_CenterHorizontal()
    {
        EditorUtils.GetSelectedComponents<RectTransform>().ForEach(it =>
        {
            Undo.RecordObject(it, "MoveCornersToAnchors_CenterHorizontal");
            it.anchoredPosition = new Vector2(0, it.anchoredPosition.y);
        });
    }

    [MenuItem("Tools/Corners to Anchors (RectTransform)/Center Vertical")]
    public static void MoveCornersToAnchors_CenterVertical()
    {
        EditorUtils.GetSelectedComponents<RectTransform>().ForEach(it =>
        {
            Undo.RecordObject(it, "MoveCornersToAnchors_CenterVertical");
            it.anchoredPosition = new Vector2(it.anchoredPosition.x, 0);
        });
    }
}
